using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI.DB;

namespace TerraEconomy.Util
{
    public class BankAccount
    {
        public int UserID { get; set; }
        public float Balance { get; set; }

        public BankAccount(int userid, float balance = 0)
        {
            UserID = userid;
            Balance = balance;
        }

        public override string ToString()
        {
            return String.Format("{{UserID: {0}, Balance: {1}}}",
                UserID,
                Balance
                );
        }

        /// <summary>
        /// Checks if the BankAccount can pay that amount
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public bool CanPay(int amount)
        {
            return (Balance - amount) >= 0;
        }

        public void InsertToDB()
        {
            try
            {
                DBHelper.Query("INSERT INTO Accounts (UserID, Balance) VALUES (@0,@1)",
                    UserID,
                    Balance
                );
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        public async Task InsertToDBasync()
        {
            try
            {
                await DBHelper.QueryAsync("INSERT INTO Accounts (UserID, Balance) VALUES (@0,@1)",
                    UserID,
                    Balance
                );
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        public void Update()
        {
            try
            {
                DBHelper.Query("UPDATE Accounts SET Balance=@0 WHERE UserID=@1",
                    Balance,
                    UserID
                );
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        public async Task UpdateAsync()
        {
            try
            {
                await DBHelper.QueryAsync("UPDATE Accounts SET Balance=@0 WHERE UserID=@1",
                    Balance,
                    UserID
                );
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        public static BankAccount GetByUserID(int userid)
        {
            using (var reader = DBHelper.QueryResult("SELECT * FROM Accounts WHERE UserID=@0",
                    userid))
            {
                while (reader.Read())
                {
                    return new BankAccount(userid, reader.Get<float>("Balance"));
                }
            }
            return null;
        }

        public static async Task<BankAccount> GetByUserIDAsync(int userid)
        {
            using (var reader = await DBHelper.QueryResultAsync("SELECT * FROM Accounts WHERE UserID=@0",
                    userid))
            {
                while (reader.Read())
                {
                    return new BankAccount(userid, reader.Get<float>("Balance"));
                }
            }
            return null;
        }

        public Transaction[] GetLastTransactions(int limit = 10)
        {
            return Transaction.GetTransactionsByReceiver(UserID, limit).Concat(Transaction.GetTransactionsBySender(UserID, limit)).ToArray();
        }
    }
}
