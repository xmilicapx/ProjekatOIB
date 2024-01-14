﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Contracts;
using Manager;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.IO;
using Microsoft.SqlServer.Server;
using static System.Net.WebRequestMethods;
using File = System.IO.File;
using System.ServiceModel;

namespace ServiceApp
{
	public class WCFService : IWCFContract
	{
		private ClientInstance _clientInstance;
		//private static readonly string projectRoute = Directory.GetParent(Environment.CurrentDirectory).FullName;
		private static readonly string projectRoute = "C:\\Users\\Dell\\Desktop\\OIB_projekat\\OIB_projekat\\ServiceApp\\bin";
		private static readonly string punishedRoute = "C:\\Users\\Dell\\Desktop\\OIB_projekat\\OIB_projekat\\ServiceApp\\bin\\Punished";

       /* private void ThrowException(string message, Exception ex = null)
        {
            string name = _clientInstance.Name;
            DateTime time = DateTime.Now;
            string exceptionMessage;
            if (ex is null)
            {
                exceptionMessage = message + String.Format("For user {0} at {1}", name, time);
                throw new FaultException<SecurityException>(new SecurityException(exceptionMessage));
            }

            exceptionMessage = message + String.Format("For user {0} at {1}; Inner exception: {2}", name, time, ex.Message);

            throw new FaultException<ServerException>(new ServerException(exceptionMessage));
        }
        */
        public void Login()
		{
			try
			{
                _clientInstance = ClientInstance.GetInstance();
                Audit.AuthenticationSuccess(_clientInstance.Name);
            }
			catch(Exception ex)
			{
                string name = "ANONYMOUS";
                DateTime time = DateTime.Now;
                string message = String.Format("Exception happened for user {0} while logging in; TIME: {1};", name, time.TimeOfDay, ex.Message);
                throw new FaultException<ServerException>(new ServerException(message));
            }
			
		}

		private bool IsPunished(string username)
		{
            var punishedUsers = File.ReadAllText(Path.Combine(punishedRoute, "spamUsers.txt"));

			if (punishedUsers.Contains(username))
				return true;
			return false;
        }

		public void SendText(byte[] dataArray)
		{
            string exceptionMessage = "Exception happened while sending message access forbidden; ";
            if (_clientInstance.Role != (int)UserRole.STUDENT)
            {
                //Console.WriteLine("Asistenti ne mogu da salju poruke ");
                Audit.AuthorizationFailed(_clientInstance.Name, "SendText", "Access forbidden");
                // ThrowException(exceptionMessage);
                

            }
            else
            {
                if (IsPunished(_clientInstance.Name))
                {
                    Audit.AuthorizationFailed(_clientInstance.Name, "SendText", "Access forbidden, user in spam");
                    var punishedMessage = "Access forbidden due to user being in spam; ";
                    //ThrowException(punishedMessage);
                }

                else
                {
                    Audit.AuthorizationSuccess(_clientInstance.Name, "SendText");

                    try
                    {
                       // Console.WriteLine("test1");
                        // padding u DecrytpedText
                        // DecrytpedText
                        string text = AESMethods.DecrytpedText(dataArray);
                        //Console.WriteLine("test2");
                        var cleanText = Extensions.RemoveNullTerminator(text);
                        //Console.WriteLine("test3");


                        byte[] buffer = Encoding.UTF8.GetBytes(cleanText);

                        string folderName = "Messages"; // Naziv foldera gdje će se čuvati poruke
                        string fileName = $"{_clientInstance.Name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt"; // Dodajemo datum i vrijeme u naziv datoteke

                        string folderPath = Path.Combine(projectRoute, folderName);
                        string filePath = Path.Combine(folderPath, fileName);

                        // Provjeravamo da li folder već postoji, ako ne, kreiramo ga
                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        // Konvertiranje byte[] u string
                        string bufferString = Encoding.UTF8.GetString(buffer);

                        // Pisanje stringa u posebnu datoteku
                        File.WriteAllText(filePath, bufferString);

                        /* using (FileStream fOutput = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
                          {
                              Console.WriteLine("test5");
                              fOutput.Write(buffer, 0, buffer.Length);
                          }*/
                    }
                    catch (Exception ex)
                    {
                        exceptionMessage = "Exception happened while writing to file; ";
                        //// ThrowException(exceptionMessage, ex);
                    }
                }
            }
			
        }

		public void PunishStudent(string username)
		{
            string exceptionMessage = "Exception happened while punishing user, access forbidden; ";

            if (_clientInstance.Role != (int)UserRole.ASISTENT)
            {
                Audit.AuthorizationFailed(_clientInstance.Name, "PunishStudent", "Access forbidden");
                // ThrowException(exceptionMessage);
            }
            else
            {
                Audit.AuthorizationSuccess(_clientInstance.Name, "PunishStudent");

                try
                {
                    var filePath = Path.Combine(punishedRoute, "spamUsers.txt");

                    using (StreamWriter sw = File.AppendText(filePath))
                        sw.Write($"{username},");

                    Audit.PunishStudent(username);
                }
                catch (Exception ex)
                {
                    exceptionMessage = "Exception happened while punishing user; ";
                    // ThrowException(exceptionMessage, ex);
                }
            }
            
        }

		public void Logout()
		{
            try
            {
                ClientInstance.WipeInstance();
                _clientInstance = null;
            }
            catch(Exception ex)
            {
                string exceptionMessage = "Exception happened while logging out; ";
               // ThrowException(exceptionMessage, ex);
            }

        }

        public Dictionary<string, List<string>> GetAll(int minCharacters = 0)
        {
            if (_clientInstance.Role != (int)UserRole.ASISTENT)
            {
                Audit.AuthorizationFailed(_clientInstance.Name, "GetAll", "Access forbidden");
                string name = _clientInstance.Name;
                DateTime time = DateTime.Now;
                string message = String.Format("Exception happened for user {0} while getting all messages, access forbidden; TIME: {1};", name, time.TimeOfDay);
                // throw new FaultException<SecurityException>(new SecurityException(message));
            }

            Audit.AuthorizationSuccess(_clientInstance.Name, "GetAll");
            
                try
                {
                    string folderName = "Messages"; // Naziv foldera

                    // Dodajemo podfolder "Poruke" na putanju
                    string porukeFolderPath = Path.Combine(projectRoute, folderName);

                    DirectoryInfo di = new DirectoryInfo(porukeFolderPath);
                    FileInfo[] files = di.GetFiles("*.txt");

                    Dictionary<string, List<string>> returnValues = new Dictionary<string, List<string>>();

                    foreach (var file in files)
                    {
                        var userName = file.Name.Split('_')[0];
                        if (!returnValues.ContainsKey(userName))
                            returnValues.Add(userName, new List<string>());

                        var text = File.ReadAllText(file.FullName);
                        text = text.Trim();


                        if (text.Length > minCharacters)
                            returnValues[userName].Add(text);
                    }

                    return returnValues;
                }

                catch (Exception ex)
                {
                    string name = _clientInstance.Name;
                    DateTime time = DateTime.Now;
                    string message = String.Format("Exception happened for user {0} while getting all messages; TIME: {1}; Inner message{2}", name, time.TimeOfDay, ex.Message);
                    throw new FaultException<SecurityException>(new SecurityException(message));
                }
            
            
        }

        public void ForgiveStudent(string username)
        {
            string exceptionMessage = "Exception happened while forgiving user, access forbidden; ";

            if (_clientInstance.Role != (int)UserRole.ASISTENT)
            {
                Audit.AuthorizationFailed(_clientInstance.Name, "ForgiveStudent", "Access forbidden");
                // ThrowException(exceptionMessage);
            }
            else
            {
                Audit.AuthorizationSuccess(_clientInstance.Name, "ForgiveStudent");

                try
                {
                    var filePath = Path.Combine(punishedRoute, "spamUsers.txt");
                    var punishedUsers = File.ReadAllText(filePath);

                    var punishedArray = punishedUsers.Split(',').ToList();

                    for (int i = 0; i < punishedArray.Count; i++)
                    {
                        if (punishedArray[i] == username)
                        {
                            punishedArray.RemoveAt(i);
                        }
                    }

                    File.WriteAllText(filePath, String.Empty);

                    foreach (var punished in punishedArray)
                    {
                        using (StreamWriter sw = File.AppendText(filePath))
                            sw.Write($"{punished},");
                    }

                    Audit.ForgiveStudent(username);

                }
                catch (Exception ex)
                {
                    exceptionMessage = "Exception happened while forgiving user; ";
                    // ThrowException(exceptionMessage, ex);
                }
            }
        }
    }
}
