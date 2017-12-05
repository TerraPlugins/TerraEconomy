# TerraEconomy
TShock Economy plugin, currently under development.

# Index
* [Commands](#commands)
* [Script Documentation](#script-documentation)
* * [Utility Classes](#utility-classes-using-terraeconomyutil)
* * * [Database](#dbhelper-database-class-source)
* * * [Transaction](#transaction-class-source)
* * * [BankAccount](#bankaccount-class-source)
* * [TerraEconomy Hooks](#terraeconomy-hooks-source)
* * [TShock Hooks](#tshock-hooks)

# Commands:
Command | Permission | Arguments | Description
--- | --- | --- | ---
/cscript | TerraEconomy.admin | script_name | Creates a script in the tshock/terra_scripts folder

# Script Documentation
## Utility Classes (`using TerraEconomy.Util;`)
### DBHelper (Database) [Class Source](TerraEconomy/Util/DBHelper.cs#L15)
To use the database class in your script you need the following:
```csharp
using TShockAPI.DB;
using MySql.Data.MySqlClient;

// If using async methods:
// using System.Threading.Tasks;
```
_Note: TerraEconomy automatically uses SQlite or MySQL based on what TShock actually uses._

Then you will be able to access the following methods:
```csharp
bool DBHelper.CreateTable(SqlTable table);
bool DBHelper.CreateTableAsync(SqlTable table);

int DBHelper.Query(string query, params object[] args);
int DBHelper.QueryAsync(string query, params object[] args);

QueryResult DBHelper.QueryResult(string query, params object[] args);
QueryResult DBHelper.QueryResultAsync(string query, params object[] args);
```

### Transaction [Class Source](TerraEconomy/Util/Transaction.cs#L12)
Class public properties:
```csharp
public int RecieverID { get; set; }
public int SenderID { get; set; } // -1 = NPC
public float Amount { get; set; }
public string Message { get; set; }
public string Hash { get; set; }
public DateTime Date { get; set; }
public bool IsMobKill { get; set; }
```
Class public methods:
```csharp
// Constructor
public Transaction(int reciever, int sender, float amount, string message);
// Note: date is in ticks
public Transaction(int reciever, int sender, float amount, string message, long date);

// Returns the latest transactions ordered by the date.
public static IEnumerable<Transaction> GetTransactionsBySender(int sender, int limit = 100);
public static IEnumerable<Transaction> GetTransactionsByReceiver(int reciever, int limit = 100);

public async Task InsertToDBAsync();
public void InsertToDB();

public static Transaction TransactionFromQueryResult(QueryResult t)
```
### BankAccount [Class Source](TerraEconomy/Util/BankAccount.cs#L11)

Class public properties:
```csharp
public int UserID { get; set; }
public float Balance { get; set; }
```
```csharp
// Constructor
public BankAccount(int userid, float balance = 0);

public static BankAccount GetByUserID(int userid);
public static async Task<BankAccount> GetByUserIDAsync(int userid);

public Transaction[] GetLastTransactions(int limit = 10);
public bool CanPay(int amount);

public void Update(); // Updates the BankAccount in the database.
public async Task UpdateAsync();

public async Task InsertToDBAsync();
public void InsertToDB();
```

## TerraEconomy Hooks [Source](TerraEconomy/Hooks)
### BankAccount Hooks [Class Source](TerraEconomy/Hooks/BankHooks.cs#L19)
```csharp
// Invoked when a transaction happens
public static event TransactionEvent OnTransaction;
// Invoked when the player logs in his bank account (TShock Login).
public static event BankAccountLogin OnBankAccountLogin;
// Invoked when the player logs out.
public static event BankAccountLogin OnBankAccountLogout;

// Delegates
public delegate void TransactionEvent(TSPlayer sender, Transaction t);
public delegate void BankAccountLogin(TSPlayer sender, BankAccount account);

// Usage:
TerraEconomy.Hooks.BankHooks.OnTransaction += MyMethod;
```

## TShock Hooks
You can hook to any tshock hook within your script:
```csharp
using TShockAPI;
TShockAPI.Hooks.PlayerHooks.PlayerChat += PlayerHooks_PlayerChat;
```

## Full Script Example
TODO: Finish the example script
```csharp
using System;

using TShockAPI;
using TShockAPI.DB;
using Newtonsoft.Json;

using MySql.Data.MySqlClient;

using TerraEconomy;
using TerraEconomy.Util;

public class Experience : TeconomyScript
{
    public override async void Initialize()
    {
        await DBHelper.CreateTableAsync(new SqlTable("Experience",
                new SqlColumn("UserID", MySqlDbType.Int32) { NotNull = true },
                new SqlColumn("Level", MySqlDbType.Int32) { DefaultValue = "1" },
                new SqlColumn("XP", MySqlDbType.Int64) { DefaultValue = "0" }
                ));

        TShockAPI.Hooks.PlayerHooks.PlayerChat += PlayerHooks_PlayerChat;
        TerraEconomy.Hooks.BankHooks.OnTransaction += OnTransaction;
    }

    public override void Dispose(bool disposing)
    {
        if (disposing)
        {
            TShockAPI.Hooks.PlayerHooks.PlayerChat -= PlayerHooks_PlayerChat;
            TerraEconomy.Hooks.BankHooks.OnTransaction -= OnTransaction;
        }
    }

    private void PlayerHooks_PlayerChat(TShockAPI.Hooks.PlayerChatEventArgs e)
    {
    }

    private void OnTransaction(TSPlayer sender, Transaction t)
    {
        // TShock.Log.ConsoleInfo(t.ToString());
    }
}
```
