﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Xml.Serialization;

namespace Tasker_Server
{
    class Server
    {
        const string minimalVersion = "1.0.0";                          //Минимальная версия клиента
        static public List<Users> users = new List<Users>();     //Список пользователей
        
        static void Main(string[] args)
        {
            Console.WriteLine("Tasker Server     Версия 1.0.0 (13.07.2019)\n");
            LoadUsers();
            Users admin = users.Find(o => o.login == "admin");
            string newpas = PasGenerate.Generate();
            if (admin == null) users.Add(new Users("admin", newpas, "Администратор"));
            else admin.password = newpas;
            SaveUsers();
            Console.WriteLine("Пароль для пользователя admin: " + newpas);

            //Запуск сервера
            TcpListener server = new TcpListener(IPAddress.Any, 8082);
            server.Start();
            Log.Write("Сервер запущен...");
            while (true)
            {
                ThreadPool.QueueUserWorkItem(call, server.AcceptTcpClient());
            }
        }

        /// <summary>
        /// Обработка запроса
        /// </summary>
        /// <param name="clientobject"></param>
        static void call(object clientobject)
        {
            try
            {
                TcpClient client = clientobject as TcpClient;
                using (NetworkStream stream = client.GetStream())
                {
                    BinaryReader reader = new BinaryReader(stream);
                    BinaryWriter writer = new BinaryWriter(stream);
                    writer.Write(answer(reader.ReadString().Split('☺')));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            };
        }

        public static void LoadUsers()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(List<Users>));
                using (var reader = new StreamReader("users.xml"))
                    users = (List<Users>)serializer.Deserialize(reader);
            }
            catch
            {
                users = new List<Users>();
            }
        }
        public static void SaveUsers()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(List<Users>));
                using (var writer = new StreamWriter("users.xml"))
                    serializer.Serialize(writer, users);
            }
            catch
            {
                Log.Write("Ошибка при сохранении данных");
            }
        }

        static string answer(string[] query)
        {
            if (query[0] == "ping") return "pong";
            if (query[0] == "login")
            {
                if (query.Count() != 4) return "error";
                if (query[1] != minimalVersion) return "badversion☺" + minimalVersion;
                Users acc = users.Find(o => o.login == query[2]);
                if (acc == null) return "failed";
#if DEBUG
                if (query[3] != "123") return "failed";
#else
                if (query[2] != acc.password) return "failed";
#endif
                Log.Write("Авторизовался пользователь " + acc.fullname);
                return "ok☺" + acc.fullname;
            }
            if (query[0] == "userlist")
            {
                string ans = "";
                foreach (Users acc in users)
                    if (acc.login != "admin")
                        ans += acc.login + "☺";
                return ans;
            }
            if (query[0] == "userread")
            {
                if (query.Count() != 2) return "error";
                Users user = users.Find(o => o.login == query[1]);
                if (user == null) return "error";
                return user.login + "☺" +
                    user.password + "☺" +
                    user.fullname + "☺" +
                    user.company + "☺" +
                    user.departament + "☺" +
                    user.post + "☺" +
                    user.rights + "☺" +
                    user.comment;
            }
            if (query[0] == "userwrite")
            {
                if (query.Count() != 9) return "error";
                Users user = users.Find(o => o.login == query[1]);
                if (user == null)
                {
                    user = new Users();
                    users.Add(user);
                }
                user.Set(query);
                SaveUsers();
                return "ok";
            }
            if (query[0] == "userdel")
            {




            }
            return "what?";
        }
    }
}
