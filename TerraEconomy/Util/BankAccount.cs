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

        public void InsertToDB()
        {
            try
            {
                Database.db.Query("INSERT INTO Accounts (UserID, Balance) VALUES (@0,@1)",
                    UserID,
                    Balance
                );
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        public static BankAccount GetByUserID(int userid)
        {
            using (var reader = Database.db.QueryReader("SELECT * FROM Accounts WHERE UserID=@0",
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
