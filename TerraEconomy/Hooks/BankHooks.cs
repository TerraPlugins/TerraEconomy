using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraEconomy.Util;
using TShockAPI;

namespace TerraEconomy.Hooks
{
    /// <summary>
    /// Class containing bank related hooks
    /// </summary>
    public static class BankHooks
    {
        public delegate void TransactionEvent(TSPlayer sender, Transaction t);
        public delegate void BankAccountLogin(TSPlayer sender, BankAccount account);

        /// <summary>
        /// Called when a transaction happens
        /// </summary>
        public static event TransactionEvent OnTransaction;
        /// <summary>
        /// Called when the player logs in his bank account (TShock Login).
        /// </summary>
        public static event BankAccountLogin OnBankAccountLogin; // TODO: Finish this event

        public static void InvokeOnTransaction(TSPlayer sender, Transaction t) => OnTransaction?.Invoke(sender, t);
        public static void InvokeOnBankAccountLogin(TSPlayer sender, BankAccount account) => OnBankAccountLogin?.Invoke(sender, account);
    }
}
