using Bank_ATM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;


namespace Bank_ATM.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        SqlConnection conn = new SqlConnection("Data Source=localhost\\SQLEXPRESS;Initial Catalog=TEST_DB;Integrated Security=True");

        public IActionResult Index(Person person)
        {
            if (HttpContext.Request.Cookies.ContainsKey("auth"))
            {
                string auth = HttpContext.Request.Cookies["auth"];
                person.cardNumber = auth.Split("%&")[1];
                person.pinNumber = auth.Split("%&")[0];
                person.balance = "0";
            }

            if (person.cardNumber == null)
            {
                return View();
            }
            string lookup = "Select @pin = pin_number, @is_locked = is_locked ,@id = id from BANK_DATA where card_number = @cardNumber;";
            int is_locked = 0;
            string pin;
            using (SqlCommand queryData = new SqlCommand(lookup))
            {
                queryData.Connection = conn;
                queryData.Parameters.Add("@cardNumber", SqlDbType.Int).Value = (person.cardNumber != null ? person.cardNumber : DBNull.Value);
                queryData.Parameters.Add(new SqlParameter("@pin", SqlDbType.Int) { Direction = ParameterDirection.Output });
                queryData.Parameters.Add(new SqlParameter("@is_locked", SqlDbType.Int) { Direction = ParameterDirection.Output });
                queryData.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Direction = ParameterDirection.Output });

                conn.Open();
                queryData.ExecuteNonQuery();
                person.id = (int)queryData.Parameters["@id"].Value;
                pin = queryData.Parameters["@pin"].Value.ToString();
                is_locked = (queryData.Parameters["@is_locked"].Value != DBNull.Value ? (int)queryData.Parameters["@is_locked"].Value : 0 );
                conn.Close();

            }
            if (is_locked >= 5)
            {
                ViewBag.msg = $"Your account has been locked!!<br> Please click <a href='/Home/UnlockAccount'>here</a> to unlock your account";
                return View();
            }


                if (person.pinNumber != null && is_locked < 5 && person.pinNumber == pin)
            {
                string updateQuery = "Update BANK_DATA set is_locked = 0 where card_number = @cardNumber;";
                using (SqlCommand queryData = new SqlCommand(updateQuery))
                {
                    queryData.Connection = conn;
                    queryData.Parameters.Add("@cardNumber", SqlDbType.Int).Value = person.cardNumber ;

                    conn.Open();
                    queryData.ExecuteNonQuery();
                    conn.Close();

                }
                if(person.stayLogged && !HttpContext.Request.Cookies.ContainsKey("auth"))
                {
                    string auth = string.Format("{1}%&{0}", person.cardNumber, person.pinNumber);
                    setCookie(auth, 120);
                }
                

                return RedirectToAction("ATMView", "Home", person);
                
            }
            else
            {
                string updateQuery = "Update BANK_DATA set is_locked += 1 where card_number = @cardNumber;";
                using (SqlCommand queryData = new SqlCommand(updateQuery))
                {
                    queryData.Connection = conn;
                    queryData.Parameters.Add("@cardNumber", SqlDbType.Int).Value = person.cardNumber;

                    conn.Open();
                    queryData.ExecuteNonQuery();
                    conn.Close();

                }
                ViewBag.msg = is_locked + 1 < 5 ? $"Pin number incorrect!<br> You have {5 - is_locked-1} tries left" : "Your account has been locked!!<br> Please click <a href='/Home/UnlockAccount'>here</a> to unlock your account";
                return View();
            }
            
        }

        public int setCookie(String auth, int expirySeconds)
        {
            string sessionId = "123";
            CookieOptions cookieOptions = new CookieOptions();
            cookieOptions.Expires = new DateTimeOffset(DateTime.Now.AddSeconds(expirySeconds));
            HttpContext.Response.Cookies.Append("auth", auth, cookieOptions);
            HttpContext.Response.Cookies.Append("sessionId", sessionId);
            return 0;
        }

        [HttpGet]
        public IActionResult UnlockAccount()
        {
            return View();
        }
        
        public IActionResult Logout()
        {
         
            if (HttpContext.Request.Cookies.ContainsKey("auth"))
            {
                CookieOptions cookieOptions = new CookieOptions();
                cookieOptions.Expires = DateTime.Now.AddDays(-1);
                HttpContext.Response.Cookies.Append("auth", "", cookieOptions);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UnlockAccount(Person person)
        {
            string lookup = " IF (Select COUNT(1) from BANK_DATA where card_number = @cardNumber and account_number = @accountNumber and pin_number = @pinNumber) > 0 " +
                " BEGIN UPDATE BANK_DATA SET is_locked = 0 where card_number  = @cardNumber; SELECT @ret = 1; END " +
                            " ELSE SELECT @ret = 0;";
            int ret = 0;           
            using (SqlCommand queryData = new SqlCommand(lookup))
            {
                queryData.Connection = conn;
                queryData.Parameters.Add("@cardNumber", SqlDbType.Int).Value = person.cardNumber;
                queryData.Parameters.Add("@accountNumber", SqlDbType.Int).Value = person.accountNumber;
                queryData.Parameters.Add("@pinNumber", SqlDbType.Int).Value = person.pinNumber;
                queryData.Parameters.Add(new SqlParameter("@ret", SqlDbType.Int) { Direction = ParameterDirection.Output });

                conn.Open();
                queryData.ExecuteNonQuery();
                ret = (int)queryData.Parameters["@ret"].Value;
                conn.Close();

            }
            if (ret == 1)
            {
                ViewBag.msg = "<p style = 'color:black;'>Unlock successful! Return to login page. </p>";
            }
            else
            {
                ViewBag.msg = "<p style = 'color:red;'>Unlock not successful! Please try again.</p>";
            }
            return View();
        }

        [HttpGet]
        public IActionResult ATMView(Person person)
        {
             string balanceEnquiry = "Select @id = id, @fname = first_name, @lname = last_name  from BANK_DATA where id = @id;";

                using (SqlCommand queryData = new SqlCommand(balanceEnquiry))
                {
                    queryData.Connection = conn;

                    queryData.Parameters.Add("@id", SqlDbType.Int).Value = person.id;
                    queryData.Parameters.Add(new SqlParameter("@fname", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output });
                    queryData.Parameters.Add(new SqlParameter("@lname", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output });

                    conn.Open();
                    queryData.ExecuteNonQuery();

                    person.firstName = queryData.Parameters["@fname"].Value.ToString();
                    person.lastName = queryData.Parameters["@lname"].Value.ToString();

                    conn.Close();

                }
            
            return View(person);

        }

       /* using (SqlCommand queryData = new SqlCommand(balanceEnquiry))
            {
                queryData.Connection = conn;
                queryData.Parameters.Add("@id", SqlDbType.Int).Value = person.id;
                queryData.Parameters.Add(new SqlParameter("@balance", SqlDbType.Int) { Direction = ParameterDirection.Output });

                conn.Open();
                queryData.ExecuteNonQuery();
                balance = (int) queryData.Parameters["@balance"].Value;

}*/

        [HttpGet]
        public IActionResult BalanceEnquiry(Person person)
        {
            
            string balanceEnquiry = "Select @balance = balance from BANK_DATA where id = @id;";

            using (SqlCommand queryData = new SqlCommand(balanceEnquiry))
            {
                queryData.Connection = conn;
                queryData.Parameters.Add("@id", SqlDbType.Int).Value = person.id;
                queryData.Parameters.Add(new SqlParameter("@balance", SqlDbType.NVarChar, 100) { Direction = ParameterDirection.Output });

                conn.Open();
                queryData.ExecuteNonQuery();
                person.balance = queryData.Parameters["@balance"].Value.ToString();

                conn.Close();

            }

            return View(person);

        }

        [HttpGet]
        public IActionResult Withdrawal(int id, string msg)
        {
            WithdrawalData data = new WithdrawalData();
            data.id = id;

            data.msg = msg;

            return View(data);
        }

        [HttpGet]
        public IActionResult AfterWithdrawal(int id, string msg)
        {
            WithdrawalData data = new WithdrawalData();
            data.id = id;
            data.msg = msg;
            return View(data);
        }

        [HttpPost]
        public IActionResult Withdrawal(WithdrawalData data)
        {            
            if(data.amount <= 0)
            {
                return RedirectToAction("Withdrawal", "Home", new { id = data.id, msg = "Please enter valid amount!" });
            }

            Person person = new Person();
            person.id = data.id;
            string lookup = "select @balance = balance from BANK_DATA  where id = @id";
            using (SqlCommand queryData = new SqlCommand(lookup))
            {
                queryData.Connection = conn;
                queryData.Parameters.Add("@id", SqlDbType.Int).Value = person.id;
                queryData.Parameters.Add(new SqlParameter("@balance", SqlDbType.Int) { Direction = ParameterDirection.Output });

                conn.Open();
                queryData.ExecuteNonQuery();
                person.balance = queryData.Parameters["@balance"].Value.ToString();
                conn.Close();
            }

            if (data.amount > float.Parse(person.balance))
            {
                return RedirectToAction("Withdrawal", "Home", new { id = data.id, msg = "Balance too low, Enter another amount" });
            }


            string withdrawal = "Update BANK_DATA set balance = balance - @amount where id = @id;" +
                                " INSERT INTO TRANSACTIONS(id, trans_type, description, amount) VALUES (@id, @type,@description, Convert(varchar, -@amount) ); " +
                " select @balance = balance, @fname = first_name, @lname = last_name from bank_data where id = @id;";
            using (SqlCommand queryData = new SqlCommand(withdrawal))
            {
                queryData.Connection = conn;
                queryData.Parameters.Add("@id", SqlDbType.Int).Value = person.id;
                queryData.Parameters.Add("@amount", SqlDbType.Int).Value = data.amount;
                queryData.Parameters.Add("@type", SqlDbType.NVarChar, 100).Value = "Debit";
                queryData.Parameters.Add("@description", SqlDbType.NVarChar, 500).Value = $"Withdrawal from ATM";
                queryData.Parameters.Add(new SqlParameter("@balance", SqlDbType.Int) { Direction = ParameterDirection.Output });
                queryData.Parameters.Add(new SqlParameter("@fname", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output });
                queryData.Parameters.Add(new SqlParameter("@lname", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output });

                conn.Open();
                queryData.ExecuteNonQuery();
                person.balance = queryData.Parameters["@balance"].Value.ToString();
                person.firstName = queryData.Parameters["@fname"].Value.ToString();
                person.lastName = queryData.Parameters["@lname"].Value.ToString();
                conn.Close();
            }
            int hundredNotes = data.amount / 100;
            data.amount%= 100;
            int twentyNotes = (data.amount) / 20;
            data.amount %= 20;
            int tenNotes = data.amount / 10;
            data.amount %= 10;
            int fiveNotes = data.amount / 5;
            data.amount %= 5;
            int oneNotes = data.amount;
           
            return RedirectToAction("AfterWithdrawal","Home", new {id = person.id, msg = $"Notes Summary:<br>$100 x {hundredNotes} = {100*hundredNotes}<br>" +
                                                                                                            $"$20 x {twentyNotes} = {20 * twentyNotes}<br>" +
                                                                                                            $"$10 x {tenNotes} = {10 * tenNotes}<br>" +
                                                                                                            $"$5 x {fiveNotes} = {5 * fiveNotes}<br>" +
                                                                                                            $"$1 x {oneNotes} = {1 * oneNotes}<br>" +
                                                                                                            $"Your current balance is {person.balance}" });
        }

        [HttpGet]
        public IActionResult CashDeposit(int id, string msg)
        {
            WithdrawalData data = new WithdrawalData();
            data.id = id;
            data.msg = msg;

            return View(data);
        }

        [HttpPost]
        public IActionResult CashDeposit(WithdrawalData data)
        {
            if (data.amount <= 0)
            {
                return RedirectToAction("CashDeposit", "Home", new { id = data.id, msg = "Please enter valid amount!" });
            }

            Person person = new Person();
            person.id = data.id;

            string withdrawal = "Update BANK_DATA set balance = balance + @amount where id = @id; "  +
                                " INSERT INTO TRANSACTIONS(id, trans_type, description, amount) VALUES (@id, @type,@description, Convert(varchar, +@amount) );" +
                " select @balance = balance, @fname = first_name, @lname = last_name from bank_data where id = @id;";
            using (SqlCommand queryData = new SqlCommand(withdrawal))
            {
                queryData.Connection = conn;
                queryData.Parameters.Add("@id", SqlDbType.Int).Value = person.id;
                queryData.Parameters.Add("@amount", SqlDbType.Int).Value = data.amount;
                queryData.Parameters.Add("@type", SqlDbType.NVarChar, 100).Value = "Credit";
                queryData.Parameters.Add("@description", SqlDbType.NVarChar, 500).Value = $"Cash Deposit at ATM";
                queryData.Parameters.Add(new SqlParameter("@balance", SqlDbType.Int) { Direction = ParameterDirection.Output });
                queryData.Parameters.Add(new SqlParameter("@fname", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output });
                queryData.Parameters.Add(new SqlParameter("@lname", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output });

                conn.Open();
                queryData.ExecuteNonQuery();
                person.balance = queryData.Parameters["@balance"].Value.ToString();
                person.firstName = queryData.Parameters["@fname"].Value.ToString();
                person.lastName = queryData.Parameters["@lname"].Value.ToString();
                conn.Close();
            }
            int hundredNotes = data.amount / 100;
            data.amount %= 100;
            int twentyNotes = (data.amount) / 20;
            data.amount %= 20;
            int tenNotes = data.amount / 10;
            data.amount %= 10;
            int fiveNotes = data.amount / 5;
            data.amount %= 5;
            int oneNotes = data.amount;

            return RedirectToAction("AfterDeposit", "Home", new
            {
                id = person.id,
                msg = $"Notes Summary:<br>$100 x {hundredNotes} = {100 * hundredNotes}<br>" +
                                                                                                            $"$20 x {twentyNotes} = {20 * twentyNotes}<br>" +
                                                                                                            $"$10 x {tenNotes} = {10 * tenNotes}<br>" +
                                                                                                            $"$5 x {fiveNotes} = {5 * fiveNotes}<br>" +
                                                                                                            $"$1 x {oneNotes} = {1 * oneNotes}<br>" +
                                                                                                            $"Your current balance is {person.balance}"
            });
        }

        [HttpGet]
        public IActionResult AfterDeposit(int id, string msg)
        {
            WithdrawalData data = new WithdrawalData();
            data.id = id;
            data.msg = msg;

            return View(data);
        }

        [HttpPost]
        public IActionResult MoneyTransferPost(Payee data)
        {
            if (data.amount <= 0)
            {
                return RedirectToAction("CashDeposit", "Home", new { id = data.id, msg = "Please enter valid amount!" });
            }

            Person person = new Person();
            person.id = data.id;
            string lookup = "select @balance = balance from BANK_DATA  where id = @id";
            using (SqlCommand queryData = new SqlCommand(lookup))
            {
                queryData.Connection = conn;
                queryData.Parameters.Add("@id", SqlDbType.Int).Value = person.id;
                queryData.Parameters.Add(new SqlParameter("@balance", SqlDbType.Int) { Direction = ParameterDirection.Output });

                conn.Open();
                queryData.ExecuteNonQuery();
                person.balance = queryData.Parameters["@balance"].Value.ToString();
                conn.Close();
            }

            if (data.amount > float.Parse(person.balance))
            {
                return RedirectToAction("MoneyTransfer", "Home", new { id = data.id, msg = "Balance too low, Enter another amount" });
            }

            string withdrawal = "update BANK_DATA set balance = CASe when account_number = @accNumber and id != @id then balance + @amount " +
                                                                    " when id = @id and account_number != @accNumber then balance -@amount end from bank_data where account_number = @accNumber or id = @id; " +
                                " INSERT INTO TRANSACTIONS(id, trans_type, description, amount) select a.id, 'Debit','Money Transferred to ' + b.first_name + ' ' + b.last_name, Convert(varchar, -@amount) from BANK_DATA A inner join BANK_DATA b on a.id = @id and b.account_number = @accNumber where a.id != b.id  union select b.id,'Credit','Money Credited by ' + a.first_name + ' ' + a.last_name, Convert(varchar, @amount) from BANK_DATA A inner join BANK_DATA b on a.id = @id and b.account_number = @accNumber where a.id != b.id; " +
                " select @balance = balance, @fname = first_name, @lname = last_name from bank_data where id = @id;";
            using (SqlCommand queryData = new SqlCommand(withdrawal))
            {
                queryData.Connection = conn;
                queryData.Parameters.Add("@id", SqlDbType.Int).Value = person.id;
                queryData.Parameters.Add("@amount", SqlDbType.Int).Value = data.amount;
                queryData.Parameters.Add("@accNumber", SqlDbType.Int).Value = data.accountNumber;
                queryData.Parameters.Add(new SqlParameter("@balance", SqlDbType.Int) { Direction = ParameterDirection.Output });
                queryData.Parameters.Add(new SqlParameter("@fname", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output });
                queryData.Parameters.Add(new SqlParameter("@lname", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output });

                conn.Open();
                queryData.ExecuteNonQuery();
                person.balance = queryData.Parameters["@balance"].Value.ToString();
                person.firstName = queryData.Parameters["@fname"].Value.ToString();
                person.lastName = queryData.Parameters["@lname"].Value.ToString();
                conn.Close();
            }
            int hundredNotes = data.amount / 100;
            data.amount %= 100;
            int twentyNotes = (data.amount) / 20;
            data.amount %= 20;
            int tenNotes = data.amount / 10;
            data.amount %= 10;
            int fiveNotes = data.amount / 5;
            data.amount %= 5;
            int oneNotes = data.amount;

            return RedirectToAction("AfterTransfer", "Home", new
            {
                id = person.id});
        }

        [HttpGet]
        public IActionResult MoneyTransfer(Payee payee)
        {
            

            return View(payee);
        }

        [HttpGet]
        public IActionResult AfterTransfer(int id, string msg)
        {
            WithdrawalData data = new WithdrawalData();
            data.id = id;
            data.msg = msg;

            return View(data);
        }

        [HttpGet]
        public IActionResult AddPayee(int id, string msg)
        {
            Payee data = new Payee();
            data.id = id;
            data.msg = msg;
            ViewBag.msg = msg;
            SqlCommand comm = new SqlCommand("select * from dbo.ACCOUNT_PAYEE WHERE ID = @id;", conn);

            using (comm)
            {
                comm.Parameters.Add("@id", SqlDbType.Int).Value = id;
                
            }

            SqlDataAdapter d = new SqlDataAdapter(comm);
            DataTable dt = new DataTable();
            d.Fill(dt);

            var ds = dt.DataSet;

            Payee payee = new Payee();

            payee.id = id;
            payee.dataset = dt.AsEnumerable();

          

            return View(payee);

        }

        [HttpPost]
        public IActionResult AddPayee(int id,int accountNumber, string msg)
        {
            string lookup = "IF (select count(1) from ACCOUNT_PAYEE where id = @id and payee_account_number = @accNumber) > 0 SELECT @ret = 1;" +
                            " ELSE IF (select count(1) from BANK_DATA where account_number = @accNumber and id != @id) > 0 BEGIN INSERT INTO ACCOUNT_PAYEE(ID, PAYEE_ACCOUNT_NUMBER, PAYEE_FIRST_NAME, PAYEE_LAST_NAME) SELECT @ID, account_number, first_name, last_name from bank_data where account_number = @accNumber; SELECT @ret = 2; END   " +
                            " ELSE SELECT  @ret = 3;";
            int ret = 0;
            using (SqlCommand queryData = new SqlCommand(lookup))
            {
                queryData.Connection = conn;
                queryData.Parameters.Add("@id", SqlDbType.Int).Value = id;
                queryData.Parameters.Add("@accNumber", SqlDbType.Int).Value = accountNumber;
                queryData.Parameters.Add(new SqlParameter("@ret", SqlDbType.Int) { Direction = ParameterDirection.Output });

                conn.Open();
                queryData.ExecuteNonQuery();
                ret = (int)queryData.Parameters["@ret"].Value;
                conn.Close();
            }
            if (ret == 1 || ret == 3)
            {
                msg = ret == 1? "Payee already exists!" : "Account Number does not exist!";
                return RedirectToAction("AddPayee", new { id = id, msg = msg });
            }
            else 
            {
                msg = "Payee added successfully!" ;
                return RedirectToAction("AddPayee", new { id = id, msg = msg });
            }

        }

        [HttpGet]
        public IActionResult RecentTransactions(int id)
        {

            SqlCommand comm = new SqlCommand("select id, Format(trans_date, 'MM/dd/yyyy HH:mm') trans_date,trans_type, description, amount   from dbo.TRANSACTIONS WHERE ID = @id;", conn);

            using (comm)
            {
                comm.Parameters.Add("@id", SqlDbType.Int).Value = id;

            }

            SqlDataAdapter d = new SqlDataAdapter(comm);
            DataTable dt = new DataTable();
            d.Fill(dt);

            var ds = dt.DataSet;

            Payee payee = new Payee();

            payee.id = id;
            payee.dataset = dt.AsEnumerable();



            return View(payee);

        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}