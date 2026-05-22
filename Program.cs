using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

class Program
{
    // Connexió global a la base de dades amb la IP de la teva màquina virtual
    private static string connectionString = "SERVER=10.2.79.103;DATABASE=kaixa;UID=cashbox_app;PASSWORD=app123;";

    // Variables per fer el seguiment de la sessió de l'usuari actual
    private static int usuariLoguejatId = 0;
    private static string nomUsuariLoguejat = "";

    static void Main(string[] args)
    {
        bool sortirAplicacio = false;

        while (!sortirAplicacio)
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("    SISTEMA BANCARI GESTKAIXA v2.5   ");
            Console.WriteLine("========================================");
            Console.Write("Usuari: ");
            string? username = Console.ReadLine();

            Console.Write("Contrasenya: ");
            string? password = LeerContrasenyaSegura();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) continue;

            // 1. CONTROL D'ACCÉS PER A L'ADMINISTRADOR
            if (username == "cashbox_app" && password == "app123")
            {
                MenuAdministrador();
            }
            // 2. CONTROL D'ACCÉS PER ALS CLIENTS
            else
            {
                if (IntentarLoginClient(username, password))
                {
                    MenuClient();
                }
                else
                {
                    Console.WriteLine("\n[!] Error: Credencials incorrectes.");
                    Console.Write("Prem 'S' per sortir o qualsevol altra tecla per reintentar... ");
                    if (Console.ReadLine()?.ToUpper() == "S") sortirAplicacio = true;
                }
            }
        }
    }

    

    static void MenuClient()
    {
        bool tancarSessio = false;
        while (!tancarSessio)
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine($"=== MENÚ CLIENT | Usuari: {nomUsuariLoguejat.ToUpper()} ===");
            Console.WriteLine("========================================");
            Console.WriteLine("1. Veure els meus comptes");
            Console.WriteLine("2. Consultar saldo total");
            Console.WriteLine("3. Veure moviments d'un compte");
            Console.WriteLine("4. Fer un ingrés");
            Console.WriteLine("5. Fer una retirada");
            Console.WriteLine("6. Sol·licitar Transferència (Nova v2.5)");
            Console.WriteLine("7. Veure alertes de seguretat");
            Console.WriteLine("0. Tancar Sessió");
            Console.WriteLine("========================================");
            Console.Write("Selecciona una opció: ");

            switch (Console.ReadLine())
            {
                case "1": VeureComptesClient(); break;
                case "2": ConsultarSaldoTotal(); break;
                case "3": VeureMovimentsCompte(); break;
                case "4": OperarCompte(true); break;  // true = Ingrés
                case "5": OperarCompte(false); break; // false = Retirada
                case "6": ClientSollicitarTransferencia(); break;
                case "7": VeureAlertesClient(); break;
                case "0": tancarSessio = true; break;
                default: Console.WriteLine("\nOpció incorrecta."); Console.ReadKey(); break;
            }
        }
    }

    static void MenuAdministrador()
    {
        bool sortirAdmin = false;
        while (!sortirAdmin)
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("       MENÚ CONTROL ADMINISTRADOR       ");
            Console.WriteLine("========================================");
            Console.WriteLine("1. Registrar nou usuari client");
            Console.WriteLine("2. Obrir nou compte bancari (IBAN AUTOMÀTIC v2.0)");
            Console.WriteLine("3. Assignar usuari client a un compte");
            Console.WriteLine("4. Llistar tots els usuaris, comptes i saldos");
            Console.WriteLine("5. Gestionar Sol·licituds Pendents (Nova v2.5)");
            Console.WriteLine("0. Sortir al Login");
            Console.WriteLine("========================================");
            Console.Write("Selecciona una opció: ");

            switch (Console.ReadLine())
            {
                case "1": AdminCrearUsuari(); break;
                case "2": AdminCrearCompteAutomatic(); break;
                case "3": AdminAssignarUsuariACompte(); break;
                case "4": AdminLlistarTot(); break;
                case "5": AdminGestionarSolicituds(); break;
                case "0": sortirAdmin = true; break;
                default: Console.WriteLine("\nOpció incorrecta."); Console.ReadKey(); break;
            }
        }
    }

    

    static bool IntentarLoginClient(string user, string pass)
    {
        string query = "SELECT id, nom, cognom FROM Usuaris WHERE username = @user AND password = SHA2(@pass, 256)";
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            try
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@user", user);
                    cmd.Parameters.AddWithValue("@pass", pass);
                    using (MySqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            usuariLoguejatId = Convert.ToInt32(r["id"]);
                            nomUsuariLoguejat = $"{r["nom"]} {r["cognom"]}";
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[!] Error crític de connexió: {ex.Message}");
                Console.ReadKey();
            }
        }
        return false;
    }

    static void VeureComptesClient()
    {
        Console.Clear();
        Console.WriteLine("=== ESTAT ELS MEUS COMPTES ===");
        Console.WriteLine("{0,-10} {1,-25} {2,-15}", "ID COMPTE", "NÚMERO DE COMPTE", "ESTAT");
        Console.WriteLine("------------------------------------------------");

        string query = @"SELECT c.id, c.numero_compte, c.estat FROM Comptes c
                         INNER JOIN UsuarisComptes uc ON c.id = uc.compte_id WHERE uc.usuari_id = @uId";
        
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            conn.Open();
            using (MySqlCommand cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@uId", usuariLoguejatId);
                using (MySqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        Console.WriteLine("{0,-10} {1,-25} {2,-15}", r["id"], r["numero_compte"], r["estat"]);
                    }
                }
            }
        }
        Console.ReadKey();
    }

    static void ConsultarSaldoTotal()
    {
        Console.Clear();
        Console.WriteLine("=== PATRIMONI I SALDO TOTAL ===");
        Console.WriteLine("{0,-25} {1,15}", "Número de Compte", "Saldo Actual");
        Console.WriteLine("----------------------------------------");

        string query = @"SELECT c.numero_compte, IFNULL(vs.saldo, 0) AS saldo FROM Comptes c
                         INNER JOIN UsuarisComptes uc ON c.id = uc.compte_id
                         LEFT JOIN VistaSaldos vs ON c.id = vs.compte_id WHERE uc.usuari_id = @uId";
        
        decimal totalPatrimoni = 0;
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            conn.Open();
            using (MySqlCommand cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@uId", usuariLoguejatId);
                using (MySqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        decimal saldo = Convert.ToDecimal(r["saldo"]);
                        totalPatrimoni += saldo;
                        Console.WriteLine("{0,-25} {1,14:C2}", r["numero_compte"], saldo);
                    }
                }
            }
        }
        Console.WriteLine("----------------------------------------");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("{0,-25} {1,14:C2}", "TOTAL DISPONIBLE:", totalPatrimoni);
        Console.ResetColor();
        Console.ReadKey();
    }

    static void VeureMovimentsCompte()
    {
        Console.Clear();
        Console.WriteLine("=== CONSULTA DE MOVIMENTS ===");
        
        LlistarComptesDropdown();

        Console.Write("\nIntrodueix l'ID del compte que vols consultar: ");
        if (int.TryParse(Console.ReadLine(), out int compteId))
        {
            string query = "SELECT data, concepte, import, saldo FROM Moviments WHERE compte_id = @cId ORDER BY id DESC";
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cId", compteId);
                    using (MySqlDataReader r = cmd.ExecuteReader())
                    {
                        Console.Clear();
                        Console.WriteLine("{0,-20} {1,-25} {2,10} {3,12}", "Data/Hora", "Concepte", "Import", "Saldo Resultant");
                        Console.WriteLine("---------------------------------------------------------------------");
                        while (r.Read())
                        {
                            decimal imp = Convert.ToDecimal(r["import"]);
                            Console.ForegroundColor = imp < 0 ? ConsoleColor.Red : ConsoleColor.Green;
                            Console.WriteLine("{0,-20} {1,-25} {2,10:C2} {3,12:C2}", 
                                Convert.ToDateTime(r["data"]).ToString("dd/MM/yyyy HH:mm"), r["concepte"], imp, Convert.ToDecimal(r["saldo"]));
                        }
                        Console.ResetColor();
                    }
                }
            }
        }
        Console.ReadKey();
    }

    static void OperarCompte(bool esIngres)
    {
        Console.Clear();
        Console.WriteLine(esIngres ? "=== CONFIGURACIÓ D'INGRÉS ===" : "=== CONFIGURACIÓ DE RETIRADA ===");
        
        LlistarComptesDropdown();
        
        Console.Write("\nID del compte a operar: ");
        if (!int.TryParse(Console.ReadLine(), out int cId)) return;

        Console.Write("Quantitat en EUROS (€): ");
        if (!decimal.TryParse(Console.ReadLine(), out decimal imp) || imp <= 0) return;

        Console.Write("Concepte del moviment: ");
        string conc = Console.ReadLine() ?? "";

        if (!esIngres) imp = -imp;

        // FASE 2: Enviem el compte_id i l'usuari_id per l'auditoria 
        string query = "INSERT INTO Moviments (compte_id, usuari_id, import, concepte) VALUES (@cId, @uId, @imp, @conc)";
        
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            try
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cId", cId);
                    cmd.Parameters.AddWithValue("@uId", usuariLoguejatId); 
                    cmd.Parameters.AddWithValue("@imp", imp);
                    cmd.Parameters.AddWithValue("@conc", conc);

                    if (cmd.ExecuteNonQuery() > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("\n[+] Operació executada i comptabilitzada correctament.");
                        Console.ResetColor();
                    }
                }
            }
            catch (MySqlException sqlEx)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[!] Denegat pel Servidor: {sqlEx.Message}");
                Console.ResetColor();
            }
        }
        Console.ReadKey();
    }

    static void VeureAlertesClient()
    {
        Console.Clear();
        Console.WriteLine("=== HISTÒRIC D'ALERTES DE SEGURETAT ===");
        
        string query = @"SELECT a.data, c.numero_compte, a.missatge FROM Alertes a
                         INNER JOIN Comptes c ON a.compte_id = c.id
                         INNER JOIN UsuarisComptes uc ON c.id = uc.compte_id WHERE uc.usuari_id = @uId ORDER BY a.id DESC";
        
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            conn.Open();
            using (MySqlCommand cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@uId", usuariLoguejatId);
                using (MySqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[{Convert.ToDateTime(r["data"]):dd/MM/yyyy HH:mm}] Compte: {r["numero_compte"]} -> ALERT: {r["missatge"]}");
                    }
                    Console.ResetColor();
                }
            }
        }
        Console.ReadKey();
    }

    static void LlistarComptesDropdown()
    {
        string query = @"SELECT c.id, c.numero_compte FROM Comptes c
                         INNER JOIN UsuarisComptes uc ON c.id = uc.compte_id WHERE uc.usuari_id = @uId AND c.estat = 'ACTIU'";
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            conn.Open();
            using (MySqlCommand cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@uId", usuariLoguejatId);
                using (MySqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read()) Console.WriteLine($" -> ID: {r["id"]} | Compte: {r["numero_compte"]}");
                }
            }
        }
    }

   

    static void AdminCrearUsuari()
    {
        Console.Clear();
        Console.WriteLine("=== FORMULARI DE NOU USUARI CLIENT ===");
        Console.Write("DNI / NIE: "); string dni = Console.ReadLine() ?? "";
        Console.Write("Nom de pila: "); string nom = Console.ReadLine() ?? "";
        Console.Write("Cognoms: "); string cog = Console.ReadLine() ?? "";
        Console.Write("Direcció Postal: "); string adr = Console.ReadLine() ?? "";
        Console.Write("Telèfon de contacte: "); string tel = Console.ReadLine() ?? "";
        Console.Write("Nom d'usuari (Login Username): "); string user = Console.ReadLine() ?? "";
        Console.Write("Contrasenya d'accés inicial: "); string pass = Console.ReadLine() ?? "";

        string query = @"INSERT INTO Usuaris (dni, nom, cognom, adreca, telefon, username, password) 
                         VALUES (@dni, @nom, @cog, @adr, @tel, @user, SHA2(@pass, 256))";

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            try
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@dni", dni);
                    cmd.Parameters.AddWithValue("@nom", nom);
                    cmd.Parameters.AddWithValue("@cog", cog);
                    cmd.Parameters.AddWithValue("@adr", adr);
                    cmd.Parameters.AddWithValue("@tel", tel);
                    cmd.Parameters.AddWithValue("@user", user);
                    cmd.Parameters.AddWithValue("@pass", pass);

                    if (cmd.ExecuteNonQuery() > 0) Console.WriteLine("\n[+] Client registrat amb èxit a la Base de Dades.");
                }
            }
            catch (Exception ex) { Console.WriteLine($"\n[!] Error en donar d'alta l'usuari: {ex.Message}"); }
        }
        Console.ReadKey();
    }

    static void AdminCrearCompteAutomatic()
    {
        Console.Clear();
        Console.WriteLine("=== OBERTURA AUTOMÀTICA DE COMPTE BANCARI ===");

        Random rnd = new Random();
        string ibanGenerat = "ES" + rnd.Next(10, 99).ToString() + "2100";
        
        for (int i = 0; i < 16; i++)
        {
            ibanGenerat += rnd.Next(0, 10).ToString();
        }

        string query = "INSERT INTO Comptes (numero_compte, estat) VALUES (@iban, 'ACTIU')";

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            try
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@iban", ibanGenerat);
                    if (cmd.ExecuteNonQuery() > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("\n[+] S'ha generat un nou compte corrent de manera totalment automatitzada.");
                        Console.WriteLine($"[+] Nou IBAN: {ibanGenerat}");
                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"\n[!] El número generat ja existia, torna-ho a intentar: {ex.Message}"); }
        }
        Console.ReadKey();
    }

    static void AdminAssignarUsuariACompte()
    {
        Console.Clear();
        Console.WriteLine("=== VINCULACIÓ CLIENT - COMPTE ===");
        Console.Write("ID de l'Usuari Client: "); int.TryParse(Console.ReadLine(), out int uId);
        Console.Write("ID del Compte Bancari: "); int.TryParse(Console.ReadLine(), out int cId);
        Console.Write("Rol de la vinculació (TITULAR / AUTORITZAT): "); string rol = Console.ReadLine()?.ToUpper() ?? "TITULAR";

        string query = "INSERT INTO UsuarisComptes (usuari_id, compte_id, rol) VALUES (@uId, @cId, @rol)";

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            try
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@uId", uId);
                    cmd.Parameters.AddWithValue("@cId", cId);
                    cmd.Parameters.AddWithValue("@rol", rol);

                    if (cmd.ExecuteNonQuery() > 0) Console.WriteLine("\n[+] Relació guardada correctament.");
                }
            }
            catch (Exception ex) { Console.WriteLine($"\n[!] Error de vinculació: {ex.Message}"); }
        }
        Console.ReadKey();
    }

    static void AdminLlistarTot()
    {
        Console.Clear();
        Console.WriteLine("=== AUDITORIA GENERAL DE CLIENTS I COMPTES ===");
        Console.WriteLine("{0,-5} {1,-20} {2,-25} {3,-12} {4,10}", "ID", "Client", "Número Compte", "Rol", "Saldo actual");
        Console.WriteLine("-----------------------------------------------------------------------------");

        string query = @"SELECT u.id, u.nom, u.cognom, c.numero_compte, uc.rol, IFNULL(vs.saldo, 0) AS saldo
                         FROM Usuaris u LEFT JOIN UsuarisComptes uc ON u.id = uc.usuari_id
                         LEFT JOIN Comptes c ON uc.compte_id = c.id LEFT JOIN VistaSaldos vs ON c.id = vs.compte_id";

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            conn.Open();
            using (MySqlCommand cmd = new MySqlCommand(query, conn))
            {
                using (MySqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        string client = $"{r["nom"]} {r["cognom"]}";
                        string compte = r["numero_compte"]?.ToString() ?? "Sense Compte";
                        string rol = r["rol"]?.ToString() ?? "-";
                        decimal saldo = Convert.ToDecimal(r["saldo"]);

                        Console.WriteLine("{0,-5} {1,-20} {2,-25} {3,-12} {4,10:C2}", r["id"], client, compte, rol, saldo);
                    }
                }
            }
        }
        Console.ReadKey();
    }

    //solicitud de mes

    static void ClientSollicitarTransferencia()
    {
        Console.Clear();
        Console.WriteLine("=== SOL·LICITUD DE TRANSFERÈNCIA PENDENT ===");
        LlistarComptesDropdown();

        Console.Write("\nSelecciona l'ID del teu compte d'origen: ");
        if (!int.TryParse(Console.ReadLine(), out int cOrigenId)) return;

        Console.Write("Introdueix l'IBAN del compte destí (24 caràcters): ");
        string ibanDesti = Console.ReadLine()?.Trim() ?? "";
        if (ibanDesti.Length != 24) { Console.WriteLine("[!] IBAN invàlid (deu tenir 24 caràcters)."); Console.ReadKey(); return; }

        Console.Write("Import a transferir (€): ");
        if (!decimal.TryParse(Console.ReadLine(), out decimal imp) || imp <= 0) return;

        Console.Write("Concepte: ");
        string conc = Console.ReadLine() ?? "";

        string query = @"INSERT INTO Solicituds (compte_origen_id, compte_desti_iban, usuari_id, import, concepte) 
                         VALUES (@cOrig, @iDest, @uId, @imp, @conc)";

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            try
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cOrig", cOrigenId);
                    cmd.Parameters.AddWithValue("@iDest", ibanDesti);
                    cmd.Parameters.AddWithValue("@uId", usuariLoguejatId); // Auditoria
                    cmd.Parameters.AddWithValue("@imp", imp);
                    cmd.Parameters.AddWithValue("@conc", conc);

                    if (cmd.ExecuteNonQuery() > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("\n[+] Sol·licitud registrada. Queda pendent de l'aprovació de l'administrador.");
                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"\n[!] Error: {ex.Message}"); }
        }
        Console.ReadKey();
    }

    static void AdminGestionarSolicituds()
    {
        Console.Clear();
        Console.WriteLine("=== GESTIÓ DE SOL·LICITUDS PENDENTS ===");
        Console.WriteLine("{0,-5} {1,-10} {2,-25} {3,-10} {4,-15}", "ID", "Orig. ID", "IBAN Destí", "Import", "Concepte");
        Console.WriteLine("-----------------------------------------------------------------------------");

        string selectQuery = "SELECT id, compte_origen_id, compte_desti_iban, import, concepte FROM Solicituds WHERE estat = 'PENDENT'";
        List<int> pendentsIds = new List<int>();

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            conn.Open();
            using (MySqlCommand cmd = new MySqlCommand(selectQuery, conn))
            using (MySqlDataReader r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    int id = Convert.ToInt32(r["id"]);
                    pendentsIds.Add(id);
                    Console.WriteLine("{0,-5} {1,-10} {2,-25} {3,-10:C2} {4,-15}", 
                        id, r["compte_origen_id"], r["compte_desti_iban"], Convert.ToDecimal(r["import"]), r["concepte"]);
                }
            }

            if (pendentsIds.Count == 0)
            {
                Console.WriteLine("\nNo hi ha sol·licituds pendents de revisió.");
                Console.ReadKey();
                return;
            }

            Console.Write("\nIntrodueix l'ID de la sol·licitud a gestionar: ");
            if (!int.TryParse(Console.ReadLine(), out int solId) || !pendentsIds.Contains(solId)) return;

            Console.Write("Vols (A)provar o (D)enegar la sol·licitud? (A/D): ");
            string accio = Console.ReadLine()?.ToUpper() ?? "";

            if (accio == "A")
            {
                string getSolData = "SELECT compte_origen_id, usuari_id, import, concepte FROM Solicituds WHERE id = @id";
                int cOrig = 0, uId = 0; decimal imp = 0; string conc = "";

                using (MySqlCommand cmdGet = new MySqlCommand(getSolData, conn))
                {
                    cmdGet.Parameters.AddWithValue("@id", solId);
                    using (MySqlDataReader r2 = cmdGet.ExecuteReader())
                    {
                        if (r2.Read())
                        {
                            cOrig = Convert.ToInt32(r2["compte_origen_id"]);
                            uId = Convert.ToInt32(r2["usuari_id"]);
                            imp = Convert.ToDecimal(r2["import"]);
                            conc = "TRANSF: " + r2["concepte"].ToString();
                        }
                    }
                }

                string insertMov = "INSERT INTO Moviments (compte_id, usuari_id, import, concepte) VALUES (@cId, @uId, @imp, @conc)";
                using (MySqlCommand cmdMov = new MySqlCommand(insertMov, conn))
                {
                    cmdMov.Parameters.AddWithValue("@cId", cOrig);
                    cmdMov.Parameters.AddWithValue("@uId", uId);
                    cmdMov.Parameters.AddWithValue("@imp", -imp); 
                    cmdMov.Parameters.AddWithValue("@conc", conc);
                    
                    try {
                        cmdMov.ExecuteNonQuery();
                        
                        string updateSol = "UPDATE Solicituds SET estat = 'APROVADA' WHERE id = @id";
                        using (MySqlCommand cmdUp = new MySqlCommand(updateSol, conn))
                        {
                            cmdUp.Parameters.AddWithValue("@id", solId);
                            cmdUp.ExecuteNonQuery();
                        }
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("\n[+] Sol·licitud APROVADA. S'han enviat els diners i tancat l'expedient.");
                        Console.ResetColor();
                    }
                    catch (MySqlException sqlEx) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\n[!] Error del Servidor (possible descobert): {sqlEx.Message}");
                        Console.ResetColor();
                    }
                }
            }
            else if (accio == "D")
            {
                string updateSol = "UPDATE Solicituds SET estat = 'DENEGADA' WHERE id = @id";
                using (MySqlCommand cmdUp = new MySqlCommand(updateSol, conn))
                {
                    cmdUp.Parameters.AddWithValue("@id", solId);
                    cmdUp.ExecuteNonQuery();
                }
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n[!] Sol·licitud DENEGADA pel departament de riscos.");
                Console.ResetColor();
            }
        }
        Console.ReadKey();
    }

    static string LeerContrasenyaSegura()
    {
        string pass = "";
        ConsoleKeyInfo key;
        do
        {
            key = Console.ReadKey(true);
            if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
            {
                pass += key.KeyChar;
                Console.Write("*");
            }
            else if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
            {
                pass = pass.Substring(0, (pass.Length - 1));
                Console.Write("\b \b");
            }
        } while (key.Key != ConsoleKey.Enter);
        Console.WriteLine();
        return pass;
    }
}