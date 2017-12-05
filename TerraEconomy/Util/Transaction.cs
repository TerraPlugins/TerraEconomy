using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TShockAPI.DB;

namespace TerraEconomy.Util
{
    public class Transaction
    {
        public int RecieverID { get; set; }
        public int SenderID { get; set; } // -1 = NPC
        public float Amount { get; set; }
        public string Message { get; set; }
        public string Hash { get; set; }
        public DateTime Date { get; set; }
        public bool IsMobKill { get; set; }

        private string TransactionSuccess = "[TerraEconomy] Transaction {0} succesfully executed.";
        private string ErrorGettingBankAccountTransaction = "[TerraEconomy] Error getting a BankAccount for the player '{0}'\nTransaction {1} cancelled and deleted.";
        private string ErrorGettingBankAccount = "[TerraEconomy] Error getting a BankAccount for the player '{0}'";

        public Transaction(int reciever, int sender, float amount, string message)
        {
            RecieverID = reciever;
            SenderID = sender;
            Amount = amount;
            Message = message;
            Date = DateTime.Now;

            var bytes = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(bytes);
                Hash = BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }

        public override string ToString()
        {
            return String.Format("{{RecieverID: {0}, SenderID: {1}, Amount: {2}, Message: {3}, Date: {4}, IsMobKill: {5}, Hash: {6}}}",
                RecieverID,
                SenderID,
                Amount,
                Message,
                Date.ToString(),
                IsMobKill,
                Hash
                );
        }

        public async Task InsertToDBAsync()
        {
            try
            {
                BankAccount sender, reciever;

                reciever = await BankAccount.GetByUserIDAsync(RecieverID);
                if (reciever == null)
                {
                    TShockAPI.TShock.Log.ConsoleError(ErrorGettingBankAccount,
                        TShockAPI.TShock.Users.GetUserByID(RecieverID).Name);
                    return;
                }

                reciever.Balance += Amount;

                if (SenderID > -1) // If its -1 it's a mob.
                {
                    sender = await BankAccount.GetByUserIDAsync(SenderID);

                    if (sender == null)
                    {
                        TShockAPI.TShock.Log.ConsoleError(ErrorGettingBankAccountTransaction,
                            TShockAPI.TShock.Users.GetUserByID(SenderID).Name, Hash);
                        return;
                    }
                    sender.Balance -= Amount;
                    await sender.UpdateAsync();
                }
                await reciever.UpdateAsync();

                await DBHelper.QueryAsync("INSERT INTO Transactions (Amount, SenderID, RecieverID, Message, Date, Hash) VALUES (@0,@1,@2,@3,@4,@5)",
                    Amount,
                    SenderID,
                    RecieverID,
                    Message,
                    Date.Ticks,
                    Hash
                );
                TShockAPI.TShock.Log.ConsoleInfo(TransactionSuccess, Hash);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        public void InsertToDB()
        {
            try
            {
                BankAccount sender, reciever;

                reciever = BankAccount.GetByUserID(RecieverID);
                if (reciever == null)
                {
                    TShockAPI.TShock.Log.ConsoleError(ErrorGettingBankAccount,
                        TShockAPI.TShock.Users.GetUserByID(RecieverID).Name);
                    return;
                }

                reciever.Balance += Amount;

                if (SenderID > -1) // If its -1 it's a mob.
                {
                    sender = BankAccount.GetByUserID(SenderID);

                    if (sender == null)
                    {
                        TShockAPI.TShock.Log.ConsoleError(ErrorGettingBankAccountTransaction,
                            TShockAPI.TShock.Users.GetUserByID(SenderID).Name, Hash);
                        return;
                    }
                    sender.Balance -= Amount;
                    sender.Update();
                }
                reciever.Update();

                DBHelper.Query("INSERT INTO Transactions (Amount, SenderID, RecieverID, Message, Date, Hash) VALUES (@0,@1,@2,@3,@4,@5)",
                    Amount,
                    SenderID,
                    RecieverID,
                    Message,
                    Date.Ticks,
                    Hash
                );
                TShockAPI.TShock.Log.ConsoleInfo(TransactionSuccess, Hash);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        public Transaction(int reciever, int sender, float amount, string message, long date) : this(reciever, sender, amount, message)
        {
            Date = new DateTime(date);
        }

        public static IEnumerable<Transaction> GetTransactionsBySender(int sender, int limit = 100)
        {
            using (var reader = DBHelper.QueryResult("SELECT * FROM Transactions WHERE SenderID=@0 ORDER BY Date DESC LIMIT @1",
                    sender, limit))
            {
                while (reader.Read())
                {
                    yield return TransactionFromQueryResult(reader);
                }
            }
            
        }

        /// <summary>
        /// Gets transactions by the receiver id
        /// </summary>
        /// <param name="reciever">The TShockAPI.DB.User.ID</param>
        /// <param name="limit">How many transactions to retrieve</param>
        /// <returns></returns>
        public static IEnumerable<Transaction> GetTransactionsByReceiver(int reciever, int limit = 100)
        {
            using (var reader = DBHelper.QueryResult("SELECT * FROM Transactions WHERE RecieverID=@0 ORDER BY Date DESC LIMIT @1",
                    reciever, limit))
            {
                while (reader.Read())
                {
                    yield return TransactionFromQueryResult(reader);
                }
            }
        }

        public static Transaction TransactionFromQueryResult(QueryResult t)
        {
            return new Transaction(
                t.Get<int>("RecieverID"),
                t.Get<int>("SenderID"),
                t.Get<float>("Amount"),
                t.Get<string>("Message"),
                t.Get<long>("Date")
                );
        }

        public static void AddTransaction(Transaction t)
        {
            try
            {
                DBHelper.Query("INSERT INTO Transactions (Amount, SenderID, RecieverID, Message, Date) VALUES (@0,@1,@2,@3,@4)",
                    t.Amount,
                    t.SenderID,
                    t.RecieverID,
                    t.Message,
                    t.Date.Ticks
                );
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        public static async void AddTransactionAsync(Transaction t)
        {
            try
            {
                await DBHelper.QueryAsync("INSERT INTO Transactions (Amount, SenderID, RecieverID, Message, Date) VALUES (@0,@1,@2,@3,@4)",
                    t.Amount,
                    t.SenderID,
                    t.RecieverID,
                    t.Message,
                    t.Date.Ticks
                );
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
