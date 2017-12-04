using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
        public DateTime Date { get; set; }
        public bool IsMobKill { get; set; }

        public Transaction(int reciever, int sender, float amount, string message)
        {
            RecieverID = reciever;
            SenderID = sender;
            Amount = amount;
            Message = message;
            Date = DateTime.Now;
        }

        public override string ToString()
        {
            return String.Format("{{RecieverID: {0}, SenderID: {1}, Amount: {2}, Message: {3}, Date: {4}, IsMobKill: {5}}}",
                RecieverID,
                SenderID,
                Amount,
                Message,
                Date.ToString(),
                IsMobKill
                );
        }

        public void InsertToDB()
        {
            try
            {
                Database.db.Query("INSERT INTO Transactions (Amount, SenderID, RecieverID, Message, Date) VALUES (@0,@1,@2,@3,@4)",
                    Amount,
                    SenderID,
                    RecieverID,
                    Message,
                    Date.Ticks
                );
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
            using (var reader = Database.db.QueryReader("SELECT * FROM Transactions WHERE SenderID=@0 ORDER BY Date DESC LIMIT @1",
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
            using (var reader = Database.db.QueryReader("SELECT * FROM Transactions WHERE RecieverID=@0 ORDER BY Date DESC LIMIT @1",
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

        public static void AddTransactionToDB(Transaction t)
        {
            try
            {
                Database.db.Query("INSERT INTO Transactions (Amount, SenderID, RecieverID, Message, Date) VALUES (@0,@1,@2,@3,@4)",
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
