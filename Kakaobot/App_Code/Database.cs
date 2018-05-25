using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;

namespace Kakaobot.App_Code
{
    public class Database
    {
        string strConn = System.Web.Configuration.WebConfigurationManager.ConnectionStrings["strConn"].ConnectionString;

        // 회원등록여부 확인
        public bool is_RegistrationDB(string user_key)
        {
            string sql = $"SELECT 1 FROM SMUser WHERE userId like '{user_key}'";

            try
            {
                using (var connection = new SqlConnection(strConn))
                {
                    connection.Open();

                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        // 결과 집합에서 첫 번째 행의 첫 번째 열이거나, 결과 집합이 비어 있을 경우 null 참조
                        if (!(cmd.ExecuteScalar() is null))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            return false;
        }

        // 회원 등록
        public bool RegistrationDB(string user_key, string content)
        {
            string[] oderArray = content.Split('/');

            string sql = $"INSERT INTO SMUser (userid, name, gradenum,platform) SELECT '{user_key}', N'{oderArray[2]}', '{oderArray[1]}', 'Kakao' WHERE NOT EXISTS (SELECT userId FROM SMUser WHERE userId like '{user_key}')";

            try
            {
                using (var connection = new SqlConnection(strConn))
                {
                    connection.Open();

                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        var rows = cmd.ExecuteNonQuery();

                        Console.WriteLine("=================================================");
                        Console.WriteLine($"{rows}줄이 INSERT 되었습니다.");
                        Console.WriteLine("=================================================");

                        return true;
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            return false;
        }

        // 동방 현재상태 확인
        public string RoomStateDB()
        {
            string replyWord = "";
            string sql = "SELECT top 1 roomstate FROM RoomState WHERE roomstate IN('open', 'close') ORDER BY inc DESC";

            try
            {
                using (var connection = new SqlConnection(strConn))
                {
                    connection.Open();

                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        var result = cmd.ExecuteScalar();

                        replyWord = result.ToString().Trim();

                        if(replyWord.Equals("open"))
                        {
                            replyWord = "열림";
                        }
                        else if(replyWord.Equals("close"))
                        {
                            replyWord = "닫힘";
                        }
                        else
                        {
                            replyWord = "에러";
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            return replyWord;
        }

        // 카드키 위치 확인
        public string CardkeyStateDB()
        {
            string replyWord = "";
            string sql = "SELECT cardNum, name FROM SMUser WHERE cardNum IN ('1334', '1918', '1334,1918') ORDER BY cardNum;";

            try
            {
                using (var connection = new SqlConnection(strConn))
                {
                    connection.Open();

                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        var result = cmd.ExecuteReader();

                        while (result.Read())
                        {
                            replyWord += $"카드키 : {result[0].ToString().Trim()} / 소유자 : {result[1].ToString().Trim()} \n";

                            Console.WriteLine($"카드키 : {result[0]} / 소유자 : {result[1]}");
                        }

                        // 마지막 개행 부분의 삭제
                        replyWord = replyWord.Substring(0, replyWord.Length - 1);
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            return replyWord;
        }

        // 동방 상태 변경
        public bool RoomOpenCloseDB(string user_key, string cardNum, string roomState)
        {
            string sql = $"INSERT INTO RoomState (roomstate, userid, name, cardNum) SELECT '{roomState}', '{user_key}', (SELECT name FROM SMUser WHERE userid = '{user_key}'), '{cardNum}' WHERE NOT EXISTS (SELECT * FROM RoomState WHERE inc = (SELECT TOP 1 inc FROM RoomState WHERE roomstate IN('open', 'close') ORDER BY inc DESC) AND roomstate = '{roomState}'); " +
                $"UPDATE SMUser SET cardNum = '0' WHERE cardNum = '{cardNum}'; " +
                $"UPDATE SMUser SET cardNum = CASE WHEN '{cardNum}' = '1334' THEN '1334' ELSE '1918' END WHERE cardNum = '1334,1918'; " +
                $"UPDATE SMUser SET cardNum = CASE WHEN (SELECT cardNum FROM SMUser WHERE userId = '{user_key}') IN('0') THEN '{cardNum}' ELSE '1334,1918' END WHERE userId = '{user_key}';";

            try
            {
                using (var connection = new SqlConnection(strConn))
                {
                    connection.Open();

                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        var rows = cmd.ExecuteNonQuery();

                        Console.WriteLine("=================================================");
                        Console.WriteLine($"{rows}줄이 INSERT, UPDATE    되었습니다.");
                        Console.WriteLine("=================================================");

                        return true;
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            return false;
        }

        // 관리자 여부 확인
        public bool is_MasterDB(string user_key)
        {
            string sql = $"SELECT 1 FROM SMUser WHERE userid = '{user_key}' AND userlevel = '1'";

            try
            {
                using (var connection = new SqlConnection(strConn))
                {
                    connection.Open();

                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        // 결과 집합에서 첫 번째 행의 첫 번째 열이거나, 결과 집합이 비어 있을 경우 null 참조
                        if (!(cmd.ExecuteScalar() is null))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            return false;
        }

        // 등록인원 목록
        public string RegistrationListDB()
        {
            string replyWord = "";
            string sql = "SELECT name, gradenum, userlevel FROM SMUser ORDER BY userlevel DESC, gradenum ASC";

            try
            {
                using (var connection = new SqlConnection(strConn))
                {
                    connection.Open();

                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        var result = cmd.ExecuteReader();
                        int count = 0;

                        while (result.Read())
                        {
                            if(result[2].ToString().Trim().Equals("1"))
                            {
                                replyWord += $"{result[0].ToString().Trim()}/{result[1].ToString().Trim()}/관리자\n";
                            }
                            else
                            {
                                replyWord += $"{result[0].ToString().Trim()}/{result[1].ToString().Trim()}/회원\n";
                            }
                            count++;
                        }

                        // 마지막 개행 부분의 삭제
                        replyWord = replyWord.Substring(0, replyWord.Length - 1);

                        replyWord = $"가입자는 총 {count}명 입니다.\n\n" + replyWord;
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            return replyWord;
        }

        // 로그 확인
        public string RoomLogDB()
        {
            string replyWord = "";
            string sql = "SELECT TOP 5 name, cardNum, roomstate, modifytime FROM RoomState ORDER BY inc DESC";

            try
            {
                using (var connection = new SqlConnection(strConn))
                {
                    connection.Open();

                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        var result = cmd.ExecuteReader();

                        while (result.Read())
                        {
                            DateTime myDate = DateTime.Parse(result[3].ToString().Trim());

                            if (result[2].ToString().Trim().Equals("open"))
                            {
                                replyWord = $"{result[0].ToString().Trim()}|{result[1].ToString().Trim()}|열림|{myDate.ToString("yy-MM-dd HH:mm")}\n" + replyWord;
                            }
                            else if (result[2].ToString().Trim().Equals("close"))
                            {
                                replyWord = $"{result[0].ToString().Trim()}|{result[1].ToString().Trim()}|닫힘|{myDate.ToString("yy-MM-dd HH:mm")}\n" + replyWord;
                            }
                            else
                            {
                                replyWord = $"{result[0].ToString().Trim()}|{result[1].ToString().Trim()}|양도|{myDate.ToString("yy-MM-dd HH:mm")}\n" + replyWord;
                            }
                        }

                        // 마지막 개행 부분의 삭제
                        replyWord = replyWord.Substring(0, replyWord.Length - 1);
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }

            return replyWord;
        }
    }
}