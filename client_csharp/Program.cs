﻿using System;
using System.Text;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace ChatClient
{
    /*Internal state struct*/
    public struct User{
        public UserId Uid {get;set;}
        public string Uname {get;set;}
        public ChatRoomId[] JoinedChatRoom {get; set;}
    }

    public struct ChatRoom{
        public ChatRoomId Cid {get;set;}
        public string Cname {get;}
        public string[] HistoryMessages {get;}

    }

    public struct UserId{
        public int Id {get;set;}
    }

    public struct ChatRoomId{
        public int Id {get;set;}
    }
    
    public struct LoginRequest{
        public string UserName {get;set;} 
    }

    public struct LoginResponse{
        public bool Success {get;set;} 
        public UserId Uid {get;set;}
        public ChatRoom[] JoinedChatRoom {get;set;}                       
	 
    }

    class ChatAPI
    {
        public static string host = "http://localhost:8080";   

        public async Task<string> Login(string userName) {
            using var client = new HttpClient();
            string resource = "/login";
            string url = host + resource;

            var request = new LoginRequest();
            request.UserName = userName;
            
            var json = JsonConvert.SerializeObject(request);
            Console.WriteLine(json);         
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            Console.WriteLine(json.Length);
            Console.WriteLine("before Exception");
            try{
                var response = await client.PostAsync(url, data);
                Task<String> resultTask = response.Content.ReadAsStringAsync();
                var result = await resultTask;
                return result;
            }
            catch(Exception e){
                Console.WriteLine("{0} Exception caught.", e);
            }
            return "-100";           
        }
    }

    interface PrintOnScreen{
        public void PrintMsg();
    }

    class Greet:PrintOnScreen{
        public void PrintMsg(){
            string msg = "Welcome to kiki's chat service. Please enter /<cmd> <param> to proceed, hit a single '&' to quit.";
            Console.WriteLine(msg);
        }
    }
            //msgToScreen.Add("cmdWarn", "Please type in a command at at beginning started with '/' or a single '&' to quit:");
            //msgToScreen.Add("cmdNotExist", "Please enter an cmd in the following list:");
            //msgToScreen.Add("cmdLsit", "login|logout");
            //msgToScreen.Add("loginFail", "We are sorry, login fail. Please try again later.");
            //msgToScreen.Add("emptyChatrooms", "You haven't joined any chatroom yet.");


    public struct InputCommand{
        public string Command {get;set;}
        public string[] Parameters {get;set;} 
    }

    interface ICommand{
        public void PrintDescription();
        public void ExecuteCommand(InputCommand inputCommand, ChatAPI chat);
    }

    class Login:ICommand{
        public void PrintDescription(){
            string description = "login the user, provide the list of chatrooms user are in. Need to provide one parameter as username.";
            Console.WriteLine(description);
        }

        public void ExecuteCommand(InputCommand inputCommand, ChatAPI chat){
            string result = chat.Login(inputCommand.Parameters[0]).Result;
            LoginResponse deserializedResult = JsonConvert.DeserializeObject<LoginResponse>(result);
            Console.WriteLine("Debug logging: uid is {0}", deserializedResult.Uid.Id);
            if(deserializedResult.Success){
                Console.WriteLine("you are now login. Please refer to following chatrooms names of your joined chatrooms:");
                if(deserializedResult.JoinedChatRoom != null){
                    Console.WriteLine(deserializedResult.JoinedChatRoom.ToString());
                }else{
                    Console.WriteLine(msgToScreen["emptyChatrooms"]);
                }
            }else{
                Console.WriteLine(msgToScreen["loginFail"]);
            }
        }
    }

    class Help:ICommand{
        public void PrintDescription(){
            string description = "list all commands the kiki's service provides.";
            Console.WriteLine(description);
        }
        public Dictionary<string, ICommand> commands {get; set;}
        public void ExecuteCommand(InputCommand inputCommand, ChatAPI chat){
            Console.WriteLine("Complete command list:");
            foreach (string command in this.commands.Keys){
                Console.Write(command + " | ");
            }
            Console.WriteLine("\neach command details as follows:");
            foreach(string command in this.commands.Keys){
                Console.WriteLine(command + ":");
                commands[command].PrintDescription();
            }          
        }
    }

    class RenderScreen{
        public static int MAX_PARAM = 4;
        public void Greeting(){            
            Greet greet = new Greet();
            greet.PrintMsg();
            
            string inLines = null;
            string[] words = null;
            string[] parameters = new string[MAX_PARAM];

            var chat = new ChatAPI();

            Dictionary<string, ICommand> commands = new Dictionary<string, ICommand>();
            Login login = new Login();
            commands.Add("login", login);

            Help help = new Help();
            help.commands = commands;
            commands.Add("help", help);

            do {
                try{
                    words = null;
                    inLines = Console.ReadLine();
                    words = inLines.Split();
                    if(words[0][0] == '/'){
                        InputCommand inputCommand = new InputCommand();
                        inputCommand.Command = words[0].Replace("/", "");
                        Array.Resize(ref parameters, words.Length - 1);
                        Array.Copy(words, 1, parameters, 0, words.Length - 1);
                        inputCommand.Parameters = parameters;

                        if(commands.ContainsKey(inputCommand.Command)){
                            commands[inputCommand.Command].ExecuteCommand(inputCommand, chat);
                        }else{
                            Console.WriteLine(msgToScreen["cmdNotExist"]);
                            Console.WriteLine(msgToScreen["cmdList"]);
                        }                      
                    }else{
                        Console.WriteLine(msgToScreen["cmdWarn"]);
                    }                  
                }
                catch(IOException e){
                    Console.WriteLine(e.Message);
                }
            }while(words[0] != "&");
        }
    }

    class Program{
        static void Main(string[] args)
        { 
            var screen = new RenderScreen();
            screen.Greeting();
            
        }
    }
}