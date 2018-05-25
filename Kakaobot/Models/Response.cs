using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Kakaobot.Models
{
    public class Response
    {
        public Message message { get; set; }
        public Keyboard keyboard { get; set; }
    }

    public class Message
    {
        public string text { get; set; }
        public Photo photo { get; set; }
        public MessageButton message_button { get; set; }
    }

    public class Photo
    {
        public string url { get; set; }
        public string width { get; set; }
        public string height { get; set; }
    }

    public class MessageButton
    {
        public string label { get; set; }
        public string url { get; set; }
    }
}