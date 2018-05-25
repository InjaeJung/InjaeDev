#r "Newtonsoft.Json"
#r "System.Configuration"
#r "System.Data"

using System;
using System.Net;
using System.Net.Http.Headers;
using System.Data.SqlClient;
using System.Configuration;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// 메인 메소드
/// </summary>
/// <param name="req"></param>
/// <param name="log"></param>
/// <returns></returns>
public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    // 리퀘스트를 JSON으로 패스
    string jsonContent = await req.Content.ReadAsStringAsync();

    // events로 Queue로 받은 JSON 형식 파일을 다시 JSON으로 변환시켜 원하는 파라메터 취득하기 위해
    var events = JObject.Parse(jsonContent).SelectToken("events");

    MessageRequest msgdata = null;
    PostbackRequest postdata = null;
    
    string messageType = null;
    string userId = null;
    string replyToken = null;
    string replyWord = null;
    
    // 해당 메시지의 type 취득
    messageType = events[0].SelectToken("type").ToString();

    // 해당 메시지의 UserId 취득
    userId = events[0].SelectToken("source").SelectToken("userId").ToString();

    // 해당 메시지의 replyToken 취득
    replyToken = events[0].SelectToken("replyToken").ToString();

    if(messageType.Equals("message")) {
        msgdata = JsonConvert.DeserializeObject<MessageRequest>(jsonContent);
    }
    else if(messageType.Equals("postback")) {
        postdata = JsonConvert.DeserializeObject<PostbackRequest>(jsonContent);
    }

    // WebApps의 프로퍼티 설정에서 데이터 취득
    var ChannelAccessToken = ConfigurationManager.AppSettings["LINE_CHANNEL_ACCESS_TOKEN"];

    log.Info($"유저 아이디(userId) : {userId}");
    log.Info($"메시지 타입(msgType) : {messageType}");

    try {
        // app설정에서 연결변수로 정의한 sqldb_connection를 가져와서 SQL SERVER와 연결
        var conString  = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
        
        using(var connection = new SqlConnection(conString))
        {
            connection.Open();

            // 가입했는지 확인(LineUser 테이블 상에 아이디가 등록 되었는지 확인)
            var sql = $"SELECT 1 FROM LineUser WHERE userId like '{userId}'";

            using (SqlCommand cmd = new SqlCommand(sql, connection))
            {
                // 결과 집합에서 첫 번째 행의 첫 번째 열이거나, 결과 집합이 비어 있을 경우 null 참조
                var rows = cmd.ExecuteScalar();

                // 미가입자 
                if (rows is null) {
                    if (messageType.Equals("message")) {
                        foreach (var item in msgdata.events) {
                            // 메시지 Text 내용 취득
                            var oderstr = item.message.text.ToString();
                            // 메시지를 ' '으로 분할해서 배열에 넣음
                            string[] oderArray = oderstr.Split(' ');

                            if(oderArray[0] == "!가입") {
                                if(oderArray.Length == 3) {
                                    string invateStr = oderArray[1] + " " + oderArray[2];
                                    log.Info($"가입자 : {oderArray[1]}, 학번 : {oderArray[2]}");
                                    InvateLineDB(userId, oderArray[1], oderArray[2], log);   
                                    replyWord = "가입이 완료되었습니다.";
                                }
                                else {
                                    replyWord = "가입형식(!가입 이름 학번)을 지켜주세요";
                                }
                            }
                            else {
                                replyWord = "가입되어있지 않습니다.";
                            }
                        }
                    }
                }
                // 가입자
                else {
                    if (messageType.Equals("message")) {
                        foreach (var item in msgdata.events) {
                            // 메시지 Text 내용 취득
                            var oderstr = item.message.text.ToString();
                            // 메시지를 ' '으로 분할해서 배열에 넣음
                            string[] oderArray = oderstr.Split(' ');

                            if(oderArray[0] == "!동방") {
                                TemplateResponse tempcontent = PresentRoomstate(replyToken, messageType, "", log);

                                // JSON형식으로 변환
                                var reqData_msg = JsonConvert.SerializeObject(tempcontent);

                                log.Info($"reqData_msg : {reqData_msg}");

                                // respons작성
                                using (var client = new HttpClient()) {
                                    // 리퀘스트 데이터를 작성
                                    // ※HttpClientで[application/json]をHTTPヘッダに追加するときは下記のコーディングじゃないとエラーになる
                                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.line.me/v2/bot/message/reply");
                                    request.Content = new StringContent(reqData_msg, Encoding.UTF8, "application/json");

                                    //　인증헤더를 추가
                                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ChannelAccessToken}");

                                    // 비동기로POST
                                    var res = await client.SendAsync(request);

                                    return req.CreateResponse(res.StatusCode);
                                }
                            }
                            else if(oderArray[0] == "!카드키") {
                                TemplateResponse tempcontent = PresentCardkeyUser(replyToken, messageType, log);

                                // JSON형식으로 변환
                                var reqData_msg = JsonConvert.SerializeObject(tempcontent);

                                log.Info($"reqData_msg : {reqData_msg}");

                                // respons작성
                                using (var client = new HttpClient()) {
                                    // 리퀘스트 데이터를 작성
                                    // ※HttpClientで[application/json]をHTTPヘッダに追加するときは下記のコーディングじゃないとエラーになる
                                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.line.me/v2/bot/message/reply");
                                    request.Content = new StringContent(reqData_msg, Encoding.UTF8, "application/json");

                                    //　인증헤더를 추가
                                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ChannelAccessToken}");

                                    // 비동기로POST
                                    var res = await client.SendAsync(request);

                                    return req.CreateResponse(res.StatusCode);
                                }
                            }
                            else if(oderArray[0] == "!가입") {
                                replyWord = "이미 가입되어 있습니다.";
                            }
                        }
                    }
                    else if (messageType.Equals("postback")) {
                        foreach (var item in postdata.events) {
                            // 메시지 Text 내용 취득
                            var oderstr = item.postback.data.ToString();
                            // 메시지를 ' '으로 분할해서 배열에 넣음
                            string[] oderArray = oderstr.Split(' ');

                            TemplateResponse tempcontent = new TemplateResponse();

                            if(oderArray.Length == 1) {
                                if(oderArray[0] == "open" || oderArray[0] == "close" ) {
                                    tempcontent = PresentRoomstate(replyToken, messageType, oderArray[0], log);
                                }
                                else if(oderArray[0] == "cardkey") {
                                    tempcontent = CardkeyStateTemplate(replyToken, oderArray[0], oderArray[0], log);
                                }
                            }
                            else if (oderArray.Length == 2) {
                                if(oderArray[0] == "open" || oderArray[0] == "close" ) {
                                    replyWord = RoomOpenClose(replyToken, userId, oderArray[0], oderArray[1], log);
                                }
                                else if(oderArray[0] == "cardkey") {
                                    replyWord = CardkeyChange(replyToken, userId, oderArray[0], oderArray[1], log);
                                }
                                break;
                            }

                            if(tempcontent == null) {
                                replyWord = "이미 열려/닫혀 있습니다.";
                            }
                            else {
                                // JSON형식으로 변환
                                var reqData_post = JsonConvert.SerializeObject(tempcontent);

                                log.Info($"reqData_post : {reqData_post}");

                                // respons작성
                                using (var client = new HttpClient()) {
                                    // 리퀘스트 데이터를 작성
                                    // ※HttpClientで[application/json]をHTTPヘッダに追加するときは下記のコーディングじゃないとエラーになる
                                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.line.me/v2/bot/message/reply");
                                    request.Content = new StringContent(reqData_post, Encoding.UTF8, "application/json");

                                    //　인증헤더를 추가
                                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ChannelAccessToken}");

                                    // 비동기로POST
                                    var res = await client.SendAsync(request);

                                    return req.CreateResponse(res.StatusCode);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    catch (Exception e)
    {
        log.Info("에러 : " + e);
    }

    // 리플라이 데이터 작성
    var content = CreateMsgResponse(replyToken, replyWord, log);

    // JSON형식으로 변환
    var reqData = JsonConvert.SerializeObject(content);

    log.Info($"리퀘스트 데이터(reqData) : {reqData}");

    // respons작성
    using (var client = new HttpClient())
    {
        // 리퀘스트 데이터를 작성
        // ※HttpClientで[application/json]をHTTPヘッダに追加するときは下記のコーディングじゃないとエラーになる
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.line.me/v2/bot/message/reply");
        request.Content = new StringContent(reqData, Encoding.UTF8, "application/json");

        //　인증헤더를 추가
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ChannelAccessToken}");

        // 비동기로POST
        var res = await client.SendAsync(request);

        return req.CreateResponse(res.StatusCode);
    }
}

// 라인봇 가입
static void InvateLineDB(string userId, string userName, string userGrade, TraceWriter log) {
    try {
        // app설정에서 연결변수로 정의한 sqldb_connection를 가져와서 SQL SERVER와 연결
        var conString  = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
        
        using(var connection = new SqlConnection(conString))
        {
            connection.Open();

            // 가입 SQL
            var sql = $"INSERT INTO LineUser (userid, name, gradenum) SELECT '{userId}', N'{userName}', '{userGrade}' WHERE NOT EXISTS (SELECT userId FROM LineUser WHERE userId like '{userId}')";

            using (SqlCommand cmd = new SqlCommand(sql, connection))
            {
                // For UPDATE, INSERT, and DELETE statements, the return value is the number of rows affected by the command. 
                var rows = cmd.ExecuteNonQuery();
                log.Info("===================================="); 
                log.Info($"{rows}줄이 Insert 되었습니다.");                
                log.Info("====================================");
            }
        }
    }
    catch (Exception e)
    {
        log.Info("에러(가입) : " + e);
    }
}

// 동아리방 상태확인
static TemplateResponse PresentRoomstate(string token, string messageType, string postdata, TraceWriter log) {
    TemplateResponse res = null;
    
    try {
        // app설정에서 연결변수로 정의한 sqldb_connection를 가져와서 SQL SERVER와 연결
        var conString  = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
        
        using(var connection = new SqlConnection(conString))
        {
            connection.Open();

            // 가입 SQL
            var sql = "SELECT top 1 roomstate FROM RoomState WHERE roomstate IN('open', 'close') ORDER BY inc DESC";

            using (SqlCommand cmd = new SqlCommand(sql, connection))
            {
                // 결과 집합에서 첫 번째 행의 첫 번째 열이거나, 결과 집합이 비어 있을 경우 null 참조 
                var result = cmd.ExecuteScalar();
                string roomState = result.ToString().Trim();

                log.Info($"동방 상태 : {roomState}");

                // 메시지 타입이 메시지일때
                if(messageType == "message") {
                    res = RoomStateTemplate(token, roomState, log);
                }
                // 메시지 타입이 포스트백일때
                else if(messageType == "postback") {
                    if(roomState != postdata) {
                        log.Info($"포스트 데이터 : {postdata}");
                        res = CardkeyStateTemplate(token, roomState, postdata, log);
                    }
                }
            }
        }
    }
    catch (Exception e)
    {
        log.Info("에러(동방) : " + e);
    }

    return res;
}

// 동방 여닫기
static string RoomOpenClose(string token, string userId, string roomState, string cardNum, TraceWriter log) {
    string replyWord = null;

    try {
        // app설정에서 연결변수로 정의한 sqldb_connection를 가져와서 SQL SERVER와 연결
        var conString  = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
        
        using(var connection = new SqlConnection(conString))
        {
            connection.Open();

            // 가입 SQL
                var sql = $"INSERT INTO RoomState (roomstate, userid, name, cardNum) SELECT '{roomState}', '{userId}', (SELECT name FROM LineUser WHERE userid = '{userId}'), '{cardNum}' WHERE NOT EXISTS (SELECT * FROM RoomState WHERE inc = (SELECT TOP 1 inc FROM RoomState WHERE roomstate IN('open', 'close') ORDER BY inc DESC) AND roomstate = '{roomState}'); UPDATE LineUser SET cardNum = '0' WHERE cardNum = '{cardNum}'; UPDATE LineUser SET cardNum = CASE WHEN '{cardNum}' = '1861' THEN '1861' ELSE '1334' END WHERE cardNum = '1861,1334'; UPDATE LineUser SET cardNum = CASE WHEN (SELECT cardNum FROM LineUser WHERE userId = '{userId}') IN('0', '{cardNum}') THEN '{cardNum}' ELSE '1861,1334' END WHERE userId = '{userId}';";

            using (SqlCommand cmd = new SqlCommand(sql, connection))
            {
                // For UPDATE, INSERT, and DELETE statements, the return value is the number of rows affected by the command. 
                var rows = cmd.ExecuteNonQuery();
                log.Info("=================================================");  
                log.Info($"{rows}줄이 Insert 되었습니다.");   
                log.Info("=================================================");  
                if(roomState == "open") {
                    replyWord = "열기 완료";
                }
                else if(roomState == "close") {
                    replyWord = "닫기 완료";
                }
                else {
                    replyWord = "에러";
                }
            }
        }
    }
    catch (Exception e)
    {
        log.Info("에러(동방) : " + e);
        replyWord = "에러";
    }

    return replyWord;
}

// 카드키 변경
static string CardkeyChange(string token, string userId, string roomState, string cardNum, TraceWriter log) {
    string replyWord = null;

    try {
        // app설정에서 연결변수로 정의한 sqldb_connection를 가져와서 SQL SERVER와 연결
        var conString  = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
        
        using(var connection = new SqlConnection(conString))
        {
            connection.Open();

            // 가입 SQL
                var sql = $"INSERT INTO RoomState (roomstate, userid, name, cardNum) SELECT '{roomState}', '{userId}', (SELECT name FROM LineUser WHERE userid = '{userId}'), '{cardNum}'; UPDATE LineUser set cardNum = '0' WHERE cardNum = '{cardNum}'; UPDATE LineUser set cardNum = CASE WHEN '{cardNum}' = '1861' THEN '1334' ELSE '1861' END WHERE cardNum = '1861,1334'; UPDATE LineUser set cardNum = CASE WHEN (SELECT cardNum FROM LineUser WHERE userid = '{userId}') IN ('0', '{cardNum}') THEN '{cardNum}' ELSE '1861,1334' END WHERE userid = '{userId}';";

            using (SqlCommand cmd = new SqlCommand(sql, connection))
            {
                // For UPDATE, INSERT, and DELETE statements, the return value is the number of rows affected by the command. 
                var rows = cmd.ExecuteNonQuery();
                log.Info("=================================================");  
                log.Info($"{rows}줄이 Insert 되었습니다.");   
                log.Info("=================================================");  
                replyWord = "변경완료";
            }
        }
    }
    catch (Exception e)
    {
        log.Info("에러(동방) : " + e);
        replyWord = "에러";
    }

    return replyWord;
}

// 현재 카드키 소유자
static TemplateResponse PresentCardkeyUser(string token, string messageType, TraceWriter log) {
    TemplateResponse res = null;
    
    try {
        // app설정에서 연결변수로 정의한 sqldb_connection를 가져와서 SQL SERVER와 연결
        var conString  = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
        
        using(var connection = new SqlConnection(conString))
        {
            connection.Open();

            // 가입 SQL
            var sql = "SELECT cardNum, name FROM LineUser WHERE cardNum IN ('1334', '1861', '1861,1334') ORDER BY cardNum;";

            using (SqlCommand cmd = new SqlCommand(sql, connection))
            {
                // CommandText를 Connection에 보내고, SqlDataReader를 빌드합니다. 
                SqlDataReader result = cmd.ExecuteReader();
                string cardkeyUser = null;

                while (result.Read()) {
                    cardkeyUser += $"카드키 : {result[0].ToString().Trim()} / 소유자 : {result[1].ToString().Trim()} \n";

                    log.Info($"카드키 : {result[0]} / 소유자 : {result[1]}");
                }

                res = CardkeyUserTemplate(token, cardkeyUser, log);
            }
        }
    }
    catch (Exception e)
    {
        log.Info("에러(동방) : " + e);
    }

    return res;
}

/// <summary>
/// 메시지 리플라이 정보 작성
/// </summary>
/// <param name="token"></param>
/// <param name="praiseWord"></param>
/// <param name="log"></param>
/// <returns></returns>
static MsgResponse CreateMsgResponse(string token, string praiseWord, TraceWriter log)
{
    MsgResponse res = new MsgResponse();
    ResMessage msg = new ResMessage();

    // 리플라이 토큰은 리퀘스트를 포함하는 리플라이 토큰을 사용함
    res.replyToken = token;
    res.messages = new List<ResMessage>();

    msg.type = "text";
    msg.text = praiseWord;
    res.messages.Add(msg);

    return res;
}

// 동방 여닫기 템플렛
static TemplateResponse RoomStateTemplate(string token, string roomState, TraceWriter log) {
    TemplateResponse res = new TemplateResponse();
    ResTemplate temp = new ResTemplate();
    TemplateContents contents = new TemplateContents();
    TemplateActions actionOpen = new TemplateActions();
    TemplateActions actionClose = new TemplateActions();

    // 리플라이 토큰은 리퀘스트를 포함하는 리플라이 토큰을 사용함
    res.replyToken = token;
    res.messages = new List<ResTemplate>();

    temp.type = "template";
    temp.altText = "동아리방 열림 확인";
    temp.template = new TemplateContents();

    contents.type = "buttons";
    if(roomState == "open") {
        contents.title = "현재 동아리방의 상태 : 열림";
    }
    else if(roomState == "close") {
        contents.title = "현재 동아리방의 상태 : 닫힘";
    }
    contents.text = "동아리방의 상태를 선택해주세요.";
    contents.actions = new List<TemplateActions>();

    actionOpen.type = "postback";
    actionOpen.label = "동방 열림";
    actionOpen.data = "open";

    actionClose.type = "postback";
    actionClose.label = "동방 닫힘";
    actionClose.data = "close";

    contents.actions.Add(actionOpen);
    contents.actions.Add(actionClose);

    temp.template = contents;
    res.messages.Add(temp);

    return res;
}

// 카드키 소유자 템플렛
static TemplateResponse CardkeyUserTemplate(string token, string cardkeyUser, TraceWriter log) {
    TemplateResponse res = new TemplateResponse();
    ResTemplate temp = new ResTemplate();
    TemplateContents contents = new TemplateContents();
    TemplateActions action = new TemplateActions();

    // 리플라이 토큰은 리퀘스트를 포함하는 리플라이 토큰을 사용함
    res.replyToken = token;
    res.messages = new List<ResTemplate>();

    temp.type = "template";
    temp.altText = "동아리방 카드키 확인";
    temp.template = new TemplateContents();

    contents.type = "buttons";
    contents.title = "현재 카드키 소유자";
    contents.text = cardkeyUser;
    contents.actions = new List<TemplateActions>();

    action.type = "postback";
    action.label = "카드키 소유자 변경";
    action.data = "cardkey";

    contents.actions.Add(action);

    temp.template = contents;
    res.messages.Add(temp);

    return res;
}

// 카드키 선택 템플렛
static TemplateResponse CardkeyStateTemplate(string token, string roomState, string postdata, TraceWriter log) {
    TemplateResponse res = new TemplateResponse();
    ResTemplate temp = new ResTemplate();
    TemplateContents contents = new TemplateContents();
    TemplateActions actionOpen = new TemplateActions();
    TemplateActions actionClose = new TemplateActions();

    // 리플라이 토큰은 리퀘스트를 포함하는 리플라이 토큰을 사용함
    res.replyToken = token;
    res.messages = new List<ResTemplate>();

    temp.type = "template";
    temp.altText = "동아리방 카드키 확인";
    temp.template = new TemplateContents();

    contents.type = "buttons";
    if(roomState == "open") {
        contents.title = "동아리방 카드키(닫기)";
    }
    else if(roomState == "close") {
        contents.title = "동아리방 카드키(열기)";
    }
    else if(roomState == "cardkey") {
        contents.title = "동아리방 카드키";
    }
    contents.text = "동아리방 카드키의 번호를 알려주세요.";
    contents.actions = new List<TemplateActions>();

    actionOpen.type = "postback";
    actionOpen.label = "1334";
    actionOpen.data = $"{postdata} 1334";

    actionClose.type = "postback";
    actionClose.label = "1861";
    actionClose.data = $"{postdata} 1861";

    contents.actions.Add(actionOpen);
    contents.actions.Add(actionClose);

    temp.template = contents;
    res.messages.Add(temp);

    return res;
}

//　리퀘스트
public class MessageRequest
{
    public List<MessageEvent> events { get; set; }
}

public class PostbackRequest
{
    public List<PostbackEvent> events { get; set; }
}


//　메세지 이벤트
public class MessageEvent
{
    public string replyToken { get; set; }
    public string type { get; set; }
    public object timestamp { get; set; }
    public Source source { get; set; }
    public Message message { get; set; }
}

//　포스트백 이벤트
public class PostbackEvent
{
    public string replyToken { get; set; }
    public string type { get; set; }
    public object timestamp { get; set; }
    public Source source { get; set; }
    public Postback postback { get; set; }
}

// 소스
public class Source
{
    public string type { get; set; }
    public string userId { get; set; }
}

// 리퀘스트 메세지
public class Message
{
    public string id { get; set; }
    public string type { get; set; }
    public string text { get; set; }
}

// 포스트백 데이터
public class Postback
{
    public string data { get; set; }
}


// 메시지 리스폰스
public class MsgResponse
{
    public string replyToken { get; set; }
    public List<ResMessage> messages { get; set; }
}

// 포스트백 리스폰스
public class PostbackResponse
{
    public string replyToken { get; set; }
    public List<ResPostback> postback { get; set; }
}

// 템플렛 리스폰스
public class TemplateResponse
{
    public string replyToken { get; set; }
    public List<ResTemplate> messages { get; set; }
}

// 리스폰스 메시지
public class ResMessage
{
    public string type { get; set; }
    public string text { get; set; }
}

// 리스폰스 포스트백
public class ResPostback
{
    public string data { get; set; }
}

// 리스폰스 템플렛
public class ResTemplate
{
    public string type { get; set; }
    public string altText { get; set; }
    public TemplateContents template { get; set; }
}

// 템플렛 내용
public class TemplateContents 
{
    public string type { get; set; }
    public string title { get; set; }
    public string text { get; set; }
    public List<TemplateActions> actions { get; set; }
}

// 템플렛 액션
public class TemplateActions
{
    public string type { get; set; }
    public string label { get; set; }
    public string data { get; set; }
}