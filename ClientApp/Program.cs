using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Security.Cryptography.X509Certificates;
using Manager;
using System.Security.Principal;
using System.Threading;
using System.Web;

namespace ClientApp
{
	public class Program
	{
        static void Main(string[] args)
        {
           
            string srvCertCN = "wcfservice";

            NetTcpBinding binding = new NetTcpBinding();
            binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Certificate;

            /// Use CertManager class to obtain the certificate based on the "srvCertCN" representing the expected service identity.
            X509Certificate2 srvCert = CertManager.GetCertificateFromStorage(StoreName.TrustedPeople, StoreLocation.LocalMachine, srvCertCN);
            //Console.WriteLine(srvCert.Subject);


            EndpointAddress address = new EndpointAddress(new Uri("net.tcp://localhost:9999/Receiver"),
                                      new X509CertificateEndpointIdentity(srvCert));
            

            using (WCFClient proxy = new WCFClient(binding, address, "asistent"))
            {
                proxy.Login();
                bool run = true;
                do
                {
                    Console.WriteLine("--------------------------------");
                    Console.WriteLine("Izaberi neku od sledecih opcija: ");
                    Console.WriteLine("1. Posalji poruku");
                    Console.WriteLine("2. Izlistaj poruke sa odredjenim brojem karaktera");
                    Console.WriteLine("3. Izlistaj sve poruke");
                    Console.WriteLine("4. Kazni studenta");
                    Console.WriteLine("5. Oprostite studentu");
                    Console.WriteLine("6. Exit");
                    Console.WriteLine("--------------------------------");

                    int input = int.Parse(Console.ReadLine());
                    Console.WriteLine("---------------------------------");
                    switch (input)
                    {
                        case 1:
                            Console.WriteLine("Unesi poruku : ");
                            var message = Console.ReadLine();
                            proxy.SendMessage(message);
                            break;
                        case 2:
                            Console.WriteLine("Unesi minimalni broj karaktera");
                            int minChars = int.Parse(Console.ReadLine());
                            Console.WriteLine("-----------------------------------");
                            proxy.GetAll(minChars);
                            Console.WriteLine("-----------------------------------");
                            break;
                        case 3:
                            Console.WriteLine("-----------------------------------");
                            proxy.GetAll();
                            Console.WriteLine("-----------------------------------");
                            break;
                        case 4:
                            Console.WriteLine("Unesite username koji cete kazniti:");
                            var userToPunish = Console.ReadLine();
                            proxy.PunishStudent(userToPunish);
                            break;
                        case 5:
                            Console.WriteLine("Unesite username kojem cete oprostiti:");
                            var userToForgive = Console.ReadLine();
                            proxy.ForgiveStudent(userToForgive);
                            break;
                        case 6:
                            Console.WriteLine("Exiting...");
                            proxy.Logout();
                            run = false;
                            break;
                    }
                } while (run);
                proxy.Logout();
            }

        }
    }
    

}
