﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace JabberBCIT {
    public class ChatHub : Hub {
        public void Send(string name, string message) {
            if (message != "")
                Clients.All.addNewMessageToPage(name, message);
        }
    }
}