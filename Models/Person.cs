using System.Data;

namespace Bank_ATM.Models
{
    public class Person
    {
        public int id { get; set; }
        public string firstName { get; set;}
        public string lastName { get; set; }
        public string accountNumber { get; set; }
        public string cardNumber { get; set; }
        public string pinNumber { get; set; }
        public string balance { get; set; }
        public bool isLocked { get; set; }
    }
    public class WithdrawalData
    {
        public int id { get; set; }
        public int amount { get; set; }
        public string msg { get; set; }
    }

    public class Payee
    {
        public int id { get; set; }
        public int accountNumber { get; set; }
        public int amount { get; set; }
        public string msg { get; set; }
        public IEnumerable<DataRow> dataset {get; set;}
    }
}
