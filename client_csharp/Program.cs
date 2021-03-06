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

    public struct SendMessageRequest{
	    public UserId SenderId {get;set;}
	    public ChatRoomId ReceiverId {get;set;}
	    public string Message {get;set;}  
    }

    public struct SendMessageResponse {
	    public bool Success {get;set;}
	    public long SentTime {get;set;}
    }

    class LocalStorageData{
        public User User = new User();

        public ChatRoomId[] TransformChatRoom(ChatRoom[] chatrooms){
            if (chatrooms == null){
                return null;
            }
            int index = 0;
            ChatRoomId[] chatRoomIds = new ChatRoomId[chatrooms.Length];

            for(index = 0; index < chatrooms.Length; index ++){
                chatRoomIds[index] = chatrooms[index].Cid;
            }
            return chatRoomIds;
        }
    }

    class ChatAPI
    {
        public const string Host = "http://localhost:8080";   

        public async Task<string> Login(string userName) {
            using var client = new HttpClient();
            string resource = "/login";
            string url = Host + resource;

            var request = new LoginRequest();
            request.UserName = userName;
            
            var json = JsonConvert.SerializeObject(request);
            //Console.WriteLine(json);         
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            //Console.WriteLine(json.Length);
            //Console.WriteLine("before Exception");
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

        public async Task<string> SendMessage(UserId senderId, ChatRoomId receiverId, string message){
            using var client = new HttpClient();
            string resource = "/send";
            string url = Host + resource;

            SendMessageRequest request = new SendMessageRequest();
            request.SenderId = senderId;
            request.ReceiverId = receiverId;
            request.Message = message;

            var json = JsonConvert.SerializeObject(request);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            try{
                var response = await client.PostAsync(url, data);
                Task<String> resultTask = response.Content.ReadAsStringAsync();
                var result = await resultTask;
                return result;
            }
            catch(Exception e){
                Console.WriteLine("{0} Exception caught.", e);
            }

            return "-101";
        }
    }

    public struct InputCommand{
        public string Command {get;set;}
        public string[] Parameters {get;set;} 
    }

    interface ICommand{
        public void PrintDescription();
        public void ExecuteCommand(InputCommand inputCommand, ChatAPI chat);
    }

    abstract class BaseCommand:ICommand { 
        abstract public void  PrintDescription();
        abstract public void ExecuteCommand(InputCommand inputCommand, ChatAPI chat);
        public LocalStorageData LocalStore = new LocalStorageData();
    }

    class Login:BaseCommand{
        const string LoginFail = "We are sorry, login fail. Please try again later.";
        const string EmptyChatrooms = "You haven't joined any chatroom yet.";
        override public void PrintDescription(){
            const string description = "login the user, provide the list of chatrooms user are in. Need to provide one parameter as username.";
            Console.WriteLine(description);
        }

        override public void ExecuteCommand(InputCommand inputCommand, ChatAPI chat){
            string result = chat.Login(inputCommand.Parameters[0]).Result;
            LoginResponse deserializedResult = JsonConvert.DeserializeObject<LoginResponse>(result);
            Console.WriteLine("Debug logging: uid is {0}", deserializedResult.Uid.Id);
            base.LocalStore.User.Uid = deserializedResult.Uid;
            base.LocalStore.User.Uname = inputCommand.Parameters[0];
            base.LocalStore.User.JoinedChatRoom = base.LocalStore.TransformChatRoom(deserializedResult.JoinedChatRoom);

            if(deserializedResult.Success){
                Console.WriteLine("you are now login. Please refer to following chatrooms names of your joined chatrooms:");
                if(deserializedResult.JoinedChatRoom != null){
                    Console.WriteLine(deserializedResult.JoinedChatRoom.ToString());
                }else{
                    Console.WriteLine(EmptyChatrooms);
                }
            }else{
                Console.WriteLine(LoginFail);
            }
        }
    }

    class Send:BaseCommand{
        override public void PrintDescription(){
            const string description = "/send <sender> <receiver> '<message>'. Send message as '<message>' to reciever. Reciver can be either an user or a chatroom.";
            Console.WriteLine(description);
        }

        override public void ExecuteCommand(InputCommand inputCommand, ChatAPI chat){
            ChatRoomId sendToChatRoomId = new ChatRoomId();
            sendToChatRoomId.Id = Int32.Parse(inputCommand.Parameters[0]);

            string result = chat.SendMessage(base.LocalStore.User.Uid, sendToChatRoomId, inputCommand.Parameters[1]).Result;
            if(result == "404"){
                Console.WriteLine("Debug logging: 404 not found");
            }else{
                SendMessageResponse deserializedResult = JsonConvert.DeserializeObject<SendMessageResponse>(result);
                Console.WriteLine("Debug logging: success is {0}, timestamp is {1}", deserializedResult.Success, deserializedResult.SentTime);
                if(deserializedResult.Success){
                    Console.WriteLine("Message sent");
                }else{
                    Console.WriteLine("Fail to send message {0} to {1}", inputCommand.Parameters[2], inputCommand.Parameters[1]);
                }
            }
        }
    }

    class Help:ICommand{
        public Dictionary<string, ICommand> Commands {get; set;}
        public void PrintDescription(){
            const string description = "list all commands the kiki's service provides.";
            Console.WriteLine(description);
        }
        public void ExecuteCommand(InputCommand inputCommand, ChatAPI chat){
            Console.WriteLine("Complete command list:");
            foreach (string command in this.Commands.Keys){
                Console.Write(command + " | ");
            }
            Console.WriteLine("\neach command details as follows:");
            foreach(string command in this.Commands.Keys){
                Console.WriteLine(command + ":");
                Commands[command].PrintDescription();
            }          
        }
    }

    class RenderScreen{
        public const int MAX_PARAM = 4;

        const string Greet = "Welcome to kiki's chat service. Please enter /<cmd> <param> to proceed, hit a single '&' to quit.";  
        const string CmdWarn = "Please type in a command at at beginning started with '/' or a single '&' to quit:";
        const string CmdNotExist = "Please enter an cmd in the following list:";
        public void Greeting(){            
            Console.WriteLine(Greet);

            string inLines = null;
            string[] words = null;
            string[] parameters = new string[MAX_PARAM];

            var chat = new ChatAPI();

            Dictionary<string, ICommand> commands = new Dictionary<string, ICommand>();
            Login login = new Login();
            commands.Add("login", login);

            Help help = new Help();
            help.Commands = commands;
            commands.Add("help", help);

            Send send = new Send();
            commands.Add("send", send);

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
                            Console.WriteLine(CmdNotExist);
                            help.ExecuteCommand(inputCommand, chat);
                        }                      
                    }else{
                        Console.WriteLine(CmdWarn);
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