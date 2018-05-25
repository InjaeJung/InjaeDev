using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Kakaobot.Controllers
{
    public class MessageController : Controller
    {

        // GET: Message
        public ActionResult Index(string user_key, string type, string content)
        {
            App_Code.Database database = new App_Code.Database();

            // 관리자 여부 확인 FLG
            bool masterFlg = database.is_MasterDB(user_key);

            // 디폴트 메뉴
            string[] def_buttons = new string[] { "!동방상태", "!카드키" };

            // 관리자 메뉴
            if (masterFlg)
            {
                def_buttons = new string[] { "!동방상태", "!카드키", "!관리자메뉴" };
            }

            Models.Message message = new Models.Message
            {
                text = "지정되지 않은 명령어 입니다."
            };

            Models.Keyboard keyboard = new Models.Keyboard
            {
                type = "buttons",
                buttons = def_buttons
            };

            if (database.is_RegistrationDB(user_key))
            {
                switch (content)
                {
                    // 동방상태 분할 메뉴
                    case "!동방상태":
                        // 동방 상태를 DB에서 가져옴
                        string roomState = database.RoomStateDB();

                        message.text = $"현재 동아리방 상태 : {roomState}";
                        keyboard.buttons = new string[] { "!동방열기", "!동방닫기", "!처음으로" };
                        break;

                    // 동방 열기 액션
                    case "!동방열기":
                        message.text = "동아리방을 열때 사용한 카드키를 선택해주세요.";
                        keyboard.buttons = new string[] { "!열기(1334)", "!열기(1918)", "!처음으로" };
                        break;
                    case "!열기(1334)":
                        if(database.RoomOpenCloseDB(user_key, "1334", "open"))
                        {
                            message.text = "성공적으로 동아리방이 열렸습니다.";
                        }
                        else
                        {
                            message.text = "동아리방 열기에 실패했습니다.";
                        }
                        break;
                    case "!열기(1918)":
                        if (database.RoomOpenCloseDB(user_key, "1918", "open"))
                        {
                            message.text = "성공적으로 동아리방이 열렸습니다.";
                        }
                        else
                        {
                            message.text = "동아리방 열기에 실패했습니다.";
                        }
                        break;

                    // 동방 닫기 액션
                    case "!동방닫기":
                        message.text = "동아리방을 닫을때 사용한 카드키를 선택해주세요.";
                        keyboard.buttons = new string[] { "!닫기(1334)", "!닫기(1918)", "!처음으로" };
                        break;
                    case "!닫기(1334)":
                        if (database.RoomOpenCloseDB(user_key, "1334", "close"))
                        {
                            message.text = "성공적으로 동아리방이 닫혔습니다.";
                        }
                        else
                        {
                            message.text = "동아리방 닫기에 실패했습니다.";
                        }
                        break;
                    case "!닫기(1918)":
                        if (database.RoomOpenCloseDB(user_key, "1918", "close"))
                        {
                            message.text = "성공적으로 동아리방이 닫혔습니다.";
                        }
                        else
                        {
                            message.text = "동아리방 닫기에 실패했습니다.";
                        }
                        break;

                    // 카드키 분할 메뉴
                    case "!카드키":
                        // 동방 상태를 DB에서 가져옴
                        string cardkeyState = database.CardkeyStateDB();

                        message.text = "현재 카드키 위치 현황입니다\n" + cardkeyState;
                        keyboard.buttons = new string[] { "!카드키변경", "!처음으로" };
                        break;
                    case "!카드키변경":
                        message.text = "소유하신 카드키를 선택해주세요.";
                        keyboard.buttons = new string[] { "!카드키(1334)", "!카드키(1918)", "!처음으로" };
                        break;
                    case "!카드키(1334)":
                        if (database.RoomOpenCloseDB(user_key, "1334", "cardkey"))
                        {
                            message.text = "성공적으로 카드키 변경이 완료되었습니다.";
                        }
                        else
                        {
                            message.text = "카드키 변경에 실패했습니다.";
                        }
                        break;
                    case "!카드키(1918)":
                        if (database.RoomOpenCloseDB(user_key, "1918", "cardkey"))
                        {
                            message.text = "성공적으로 카드키 변경이 완료되었습니다.";
                        }
                        else
                        {
                            message.text = "카드키 변경에 실패했습니다.";
                        }
                        break;

                    // 관리자 메뉴
                    case "!관리자메뉴":
                        if(masterFlg)
                        {
                            message.text = "관리자 메뉴입니다.";
                            keyboard.buttons = new string[] { "!등록인원목록", "!로그출력", "!처음으로" };
                        }
                        else
                        {
                            message.text = "관리자 권한이 없습니다.";
                        }
                        break;
                    case "!등록인원목록":
                        if (masterFlg)
                        {
                            message.text = database.RegistrationListDB();
                            keyboard.buttons = new string[] { "!등록인원목록", "!로그출력", "!처음으로" };
                        }
                        else
                        {
                            message.text = "관리자 권한이 없습니다.";
                        }
                        break;
                    case "!로그출력":
                        if (masterFlg)
                        {
                            message.text = "최근 5건의 로그를 가져옵니다.\n\n" + database.RoomLogDB();
                            keyboard.buttons = new string[] { "!등록인원목록", "!로그출력", "!처음으로" };
                        }
                        else
                        {
                            message.text = "관리자 권한이 없습니다.";
                        }
                        break;

                    case "!처음으로":
                        message.text = "처음으로 돌아갑니다.";
                        break;

                    default:
                        message.text = "지정되지 않은 명령어 입니다.";
                        break;
                }
            }
            else
            {
                if(content.Equals("!등록하기"))
                {
                    message.text = "학번과 이름을 다음 예와 같이 입력해주세요.\n예) !등록/20xxxxxxx/ㅇㅇㅇ";
                    keyboard.type = "text";
                    keyboard.buttons = null;
                }
                else if(content.Length > 3 && content.Substring(0,3).Equals("!등록"))
                {
                    string[] oderArray = content.Split('/');

                    if(oderArray[1].Length != 9 || oderArray[1].Substring(0, 2) != "20" || !(Int32.TryParse(oderArray[1], out int i)))
                    {
                        message.text = "학번의 입력이 잘못되었습니다.\n예) !등록/20xxxxxxx/ㅇㅇㅇ";
                        keyboard.type = "text";
                        keyboard.buttons = null;
                    }
                    else if (oderArray[2].Length > 5 || Int32.TryParse(oderArray[2], out int j))
                    {
                        message.text = "이름의 입력이 잘못되었습니다.\n예) !등록/20xxxxxxx/ㅇㅇㅇ";
                        keyboard.type = "text";
                        keyboard.buttons = null;
                    }
                    else
                    {
                        if(database.RegistrationDB(user_key, content))
                        {
                            message.text = "등록에 성공하였습니다.";
                        }
                        else
                        {
                            message.text = "등록에 실패하였습니다.";
                            keyboard.buttons = new string[] { "!등록하기" };
                        } 
                    }
                }
                else
                {
                    message.text = "등록되지 않은 유저입니다. 회원 등록 후에 이용해주세요.";
                    keyboard.buttons = new string[] { "!등록하기" };
                }   
            }


            Models.Response response = new Models.Response
            {
                message = message,
                keyboard = keyboard
            };

            return Json(response, JsonRequestBehavior.AllowGet);
        }
    }
}