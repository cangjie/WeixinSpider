using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Web.Script.Serialization;
using System.IO;
using System.Net;
using System.Xml;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
/// <summary>
/// Summary description for Util
/// </summary>
public class Util
{
	public Util()
	{
		//
		// TODO: Add constructor logic here
		//
	}

    protected static string token = "";

    protected static DateTime tokenTime = DateTime.MinValue;
    public static string conStr = System.Configuration.ConfigurationSettings.AppSettings["constr"].Trim();

    public static string domainName = System.Configuration.ConfigurationSettings.AppSettings["domain_name"].Trim();

    public static bool IsCellNumber(string number)
    {
        bool ret = false;
        number = number.Trim();
        string regCmccStr = @"^(134[012345678]\d{7}|1[34578][012356789]\d{8})$";
        string regCuccStr = @"^1[34578][01256]\d{8}$";
        string regCtccStr = @"^1[3578][01379]\d{8}$";
        Regex regCmcc = new Regex(regCmccStr);
        Regex regCucc = new Regex(regCuccStr);
        Regex regCtcc = new Regex(regCtccStr);
        if (regCmcc.IsMatch(number) || regCtcc.IsMatch(number) || regCucc.IsMatch(number))
        {
            ret = true;
        }
        else
        {
            ret = false;
        }
        return ret;
    }

    public static bool IsNumeric(string number)
    {
        Regex reg = new Regex(@"^([1-9]\d*|0)$");
        return reg.IsMatch(number);
    }

    public static string GetSHA1(string str)
    {
        SHA1 sha = SHA1.Create();
        ASCIIEncoding enc = new ASCIIEncoding();
        byte[] bArr = enc.GetBytes(str);
        bArr = sha.ComputeHash(bArr);
        string validResult = "";
        for (int i = 0; i < bArr.Length; i++)
        {
            validResult = validResult + bArr[i].ToString("x").PadLeft(2, '0');
        }
        return validResult.Trim();
    }

    public static string GetMd5(string str)
    {
        System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
        byte[] bArr = md5.ComputeHash(Encoding.UTF8.GetBytes(str));
        string ret = "";
        foreach (byte b in bArr)
        {
            ret = ret + b.ToString("x").PadLeft(2, '0');
        }
        return ret;
    }

    public static string GetLongTimeStamp(DateTime currentDateTime)
    {
        TimeSpan ts = currentDateTime - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(ts.TotalMilliseconds).ToString();
    }

    public static string GetSafeRequestValue(HttpRequest request, string parameterName, string defaultValue)
    {
        return ((request[parameterName] == null) ? defaultValue : request[parameterName].Trim()).Replace("'","");
    }

    

    public static string GetWebContent(string url, string method, string content, string contentType)
    {
        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
        req.Method = method.Trim();
        req.ContentType = contentType;
        if (!content.Trim().Equals(""))
        {
            StreamWriter sw = new StreamWriter(req.GetRequestStream());
            sw.Write(content);
            sw.Close();
        }
        HttpWebResponse res = (HttpWebResponse)req.GetResponse();
        Stream s = res.GetResponseStream();
        StreamReader sr = new StreamReader(s);
        string str = sr.ReadToEnd();
        sr.Close();
        s.Close();
        res.Close();
        req.Abort();
        return str;
    }

    public static string GetWebContent(string url)
    {
        return GetWebContent(url, "GET", "", "html/text");
    }

    public static string UploadImageToWeixin(string path, string token)
    {
        List<FormItem> list = new List<FormItem>();

        list.Add(new FormItem()
        {
            Name = "access_token",
            ParamType = ParamType.Text,
            Value = token
        });
        //添加FORM表单中这条数据的类型，目前只做了两种，一种是文本，一种是文件
        list.Add(new FormItem()
        {
            Name = "type",
            Value = "image",
            ParamType = ParamType.Text
        });
        //添加Form表单中文件的路径，路径必须是基于硬盘的绝对路径
        list.Add(new FormItem()
        {
            Name = "media",
            Value = path,
            ParamType = ParamType.File
        });
        //通过Funcs静态类中的PostFormData方法，将表单数据发送至http://file.api.weixin.qq.com/cgi-bin/media/upload腾讯上传下载文件接口
        string jsonStr = Funcs.PostFormData(list, "http://file.api.weixin.qq.com/cgi-bin/media/upload");
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        Dictionary<string, object> json = (Dictionary<string, object>)serializer.DeserializeObject(jsonStr);
        object v;
        json.TryGetValue("media_id", out v);
        string mediaId = v.ToString();
        return mediaId;
    }

    public static string GetQrCodeTicketTemp(string token, long scene)
    {
        HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://api.weixin.qq.com/cgi-bin/qrcode/create?access_token=" + token);
        req.Method = "post";
        req.ContentType = "raw";
        //Stream streamReq = req.GetRequestStream();
        StreamWriter sw = new StreamWriter(req.GetRequestStream());
        sw.Write("{\"expire_seconds\": 1800, \"action_name\": \"QR_SCENE\", \"action_info\": {\"scene\": {\"scene_id\":" + scene.ToString().PadLeft(32,'0') + "}}}");
        sw.Close();
        sw = null;

        HttpWebResponse res = (HttpWebResponse)req.GetResponse();
        Stream s = res.GetResponseStream();
        StreamReader sr = new StreamReader(s);
        string strTicketJson = sr.ReadToEnd();
        sr.Close();
        s.Close();
        sr = null;
        s = null;
        res.Close();
        res = null;
        req.Abort();
        req = null;

        JavaScriptSerializer serializer = new JavaScriptSerializer();
        Dictionary<string, object> json = (Dictionary<string, object>)serializer.DeserializeObject(strTicketJson);
        object v;
        json.TryGetValue("ticket", out v);
        token = v.ToString();
        return token;
    }

    public static byte[] GetQrCodeByTicket(string ticket)
    {
        byte[] bArrTmp = new byte[1024 * 1024 * 10];
        HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://mp.weixin.qq.com/cgi-bin/showqrcode?ticket=" + ticket);
        HttpWebResponse res = (HttpWebResponse)req.GetResponse();
        Stream s = res.GetResponseStream();
        int i = 0;
        int j = s.ReadByte();
        for (; j != -1; i++)
        {
            bArrTmp[i] = (byte)j;
            j = s.ReadByte();
        }
        byte[] bArr = new byte[i];
        for (j = 0; j < i; j++)
        {
            bArr[j] = bArrTmp[j];
        }
        res.Close();
        req.Abort();
        return bArr;
        
    }

    public static bool SaveBytesToFile(string path, byte[] bArr)
    {
        if (File.Exists(path))
        {
            return false;
        }
        else
        {
            Stream s = File.Create(path);
            s.Write(bArr, 0, bArr.Length);
            s.Close();
            return true;
        }
    }

    public static string GetAccessToken(string appId, string appSecret)
    {
        string token = "";
        string url = "https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid="+ appId.Trim() +"&secret=" + appSecret.Trim() ;
        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
        HttpWebResponse res = (HttpWebResponse)req.GetResponse();
        Stream s = res.GetResponseStream();
        string ret = (new StreamReader(s)).ReadToEnd();
        s.Close();
        res.Close();
        req.Abort();

        JavaScriptSerializer serializer = new JavaScriptSerializer();
        Dictionary<string, object> json = (Dictionary<string, object>)serializer.DeserializeObject(ret);
        object v;
        json.TryGetValue("access_token", out v);
        token = v.ToString();

        return token;
    }

    public static string GetToken()
    {
        DateTime nowDate = DateTime.Now;
        if (nowDate - tokenTime > new TimeSpan(0, 10, 0))
        {
            token = GetAccessToken(System.Configuration.ConfigurationSettings.AppSettings["wxappid"].Trim(),
                System.Configuration.ConfigurationSettings.AppSettings["wxappsecret"].Trim());
            tokenTime = nowDate;
        }
        return token;
    }

    public static string Get2014SummerCampImageForWexinNews()
    {
        string xmlStr = "<item>"
            + "<Title><![CDATA[2014年知心姐姐北京夏令营图片集锦]]></Title>"
            + "<Description><![CDATA[看到最后有惊喜……你会看到从未见过的神奇场景！]]></Description>"
            + "<PicUrl><![CDATA[http://img.luqinwenda.com/tianjie.jpg]]></PicUrl>"
            + "<Url><![CDATA[http://img.luqinwenda.com/mag/beijing/index.html]]></Url>"
            + "</item>"
            + "<item>"
            + "<Title><![CDATA[2014年知心姐姐草原发现之旅图片集锦]]></Title>"
            + "<Description><![CDATA[2014年知心姐姐“勇敢者小使者”草原发现之旅纪录片]]></Description>"
            + "<PicUrl><![CDATA[http://img.luqinwenda.com/caoyuan1.jpeg]]></PicUrl>"
            + "<Url><![CDATA[http://img.luqinwenda.com/mag/neimeng/index.html]]></Url>"
            + "</item>"
            + "<item>"
            + "<Title><![CDATA[2014年知心姐姐鸡公山夏令营图片集锦]]></Title>"
            + "<Description><![CDATA[听，孩子们的呐喊声，你知道他们在喊什么吗？]]></Description>"
            + "<PicUrl><![CDATA[http://img.luqinwenda.com/jgs.jpg]]></PicUrl>"
            + "<Url><![CDATA[http://img.luqinwenda.com/mag/jigongshan/index.html]]></Url>"
            + "</item>"
            + "<item>"
            + "<Title><![CDATA[2014年知心姐姐秦岭自然体验营图片集锦]]></Title>"
            + "<Description><![CDATA[六天五晚的时光，秦岭的印象，大自然的声音，孩子们的欢声笑语，一点一滴都浓缩在这个短片中。现在让我们来重温这些美好的记忆吧！]]></Description>"
            + "<PicUrl><![CDATA[http://img.luqinwenda.com/ql.jpg]]></PicUrl>"
            + "<Url><![CDATA[http://img.luqinwenda.com/mag/qinling/index.html]]></Url>"
            + "</item>";
        return xmlStr;
    }

    public static string Get2014SummerCampVideoForWexinNews()
    {
        string xmlStr = "<item>"
            + "<Title><![CDATA[2014年“放飞梦想我能行”知心姐姐北京营纪录片]]></Title>"
            + "<Description><![CDATA[看到最后有惊喜……你会看到从未见过的神奇场景！]]></Description>"
            + "<PicUrl><![CDATA[http://img.luqinwenda.com/beijing.jpeg]]></PicUrl>"
            + "<Url><![CDATA[http://mp.weixin.qq.com/s?__biz=MzA3MTM1OTIwNg==&mid=201013628&idx=1&sn=2d30e27d8a9b3dbbe6d61910f1a67399#rd]]></Url>"
            + "</item>"
            + "<item>"
            + "<Title><![CDATA[2014年“勇敢小使者”知心姐姐草原发现之旅纪录片]]></Title>"
            + "<Description><![CDATA[2014年知心姐姐“勇敢者小使者”草原发现之旅纪录片]]></Description>"
            + "<PicUrl><![CDATA[http://img.luqinwenda.com/caoyuan1.jpeg]]></PicUrl>"
            + "<Url><![CDATA[http://mp.weixin.qq.com/s?__biz=MzA3MTM1OTIwNg==&mid=201035393&idx=1&sn=1c37602ed48f04a2ae29fc87dc71ce8c#rd]]></Url>"
            + "</item>"
            + "<item>"
            + "<Title><![CDATA[2014年“快乐生存，我真了不起”知心姐姐鸡公山营纪录片]]></Title>"
            + "<Description><![CDATA[听，孩子们的呐喊声，你知道他们在喊什么吗？]]></Description>"
            + "<PicUrl><![CDATA[http://img.luqinwenda.com/jigongshan.jpeg]]></PicUrl>"
            + "<Url><![CDATA[http://mp.weixin.qq.com/s?__biz=MzA3MTM1OTIwNg==&mid=201082003&idx=1&sn=919fe437486f1f5370be88ecd564c187#rd]]></Url>"
            + "</item>"
            + "<item>"
            + "<Title><![CDATA[2014年“快乐生存，秦岭自然体验营”，知心姐姐探秦岭龙脉，访西安古城纪录片]]></Title>"
            + "<Description><![CDATA[六天五晚的时光，秦岭的印象，大自然的声音，孩子们的欢声笑语，一点一滴都浓缩在这个短片中。现在让我们来重温这些美好的记忆吧！]]></Description>"
            + "<PicUrl><![CDATA[http://img.luqinwenda.com/qinling.jpeg]]></PicUrl>"
            + "<Url><![CDATA[http://mp.weixin.qq.com/s?__biz=MzA3MTM1OTIwNg==&mid=201121027&idx=1&sn=78be8563d12eb4850a091f2d6765ff91#rd]]></Url>"
            + "</item>"
            + "<item>"
            + "<Title><![CDATA[2014年世贸天阶梦想放飞仪式纪录片]]></Title>"
            + "<Description><![CDATA[世贸天阶无数孩子一起分享的震撼视频！]]></Description>"
            + "<PicUrl><![CDATA[http://img.luqinwenda.com/shimaotianjie.jpg]]></PicUrl>"
            + "<Url><![CDATA[http://mp.weixin.qq.com/s?__biz=MzA3MTM1OTIwNg==&mid=201019678&idx=1&sn=c28b197dfe37961c404c92ee9a7e2b18#rd]]></Url>"
            + "</item>";
        return xmlStr;
    }

    public static string GetMenuWodeHit(string openId, string imageUrl, int urlId, string title, string description)
    {
        string key = System.Configuration.ConfigurationSettings.AppSettings["md5key"].Trim();
        string timeStamp = GetTimeStamp();
        string xmlStr = "<item>"
            + "<Title><![CDATA[" + title + "]]></Title>"
            + "<Description><![CDATA[" + description + "]]></Description>"
            + "<PicUrl><![CDATA[" + imageUrl + "]]></PicUrl>"
            + "<Url><![CDATA[http://www.luqinwenda.com/index.php?app=public&mod=LandingPage&act=Landing&url=" + urlId.ToString() + "&openid=" + openId + "&time=" + timeStamp + "&code=" + GetMd5(openId+timeStamp+key) + "]]></Url>"
            + "</item>";
        return xmlStr;
    }

    public static string GetMenuBaomingHit()
    {
        string xmlStr = "<item>"
            + "<Title><![CDATA[“大开眼界”知心姐姐新加坡文明之旅]]></Title>"
            + "<Description><![CDATA[“大开眼界”知心姐姐新加坡文明之旅]]></Description>"
            + "<PicUrl><![CDATA[https://mmbiz.qlogo.cn/mmbiz/2x9sALwpIbXg4UswX9fFJSiaNdG2gWfdzQduE5qzzWBJibY7BCdBiakpbxlQHJfbwy3Kg9aGCJhUAh96dzt1CtpTA/0]]></PicUrl>"
            + "<Url><![CDATA[http://mp.weixin.qq.com/s?__biz=MzA3MTM1OTIwNg==&mid=201937614&idx=1&sn=33d99d7bd17fd46212940c7e21c92ed8#rd]]></Url>"
            + "</item>"
            + "<item>"
            + "<Title><![CDATA[“开启学习兴趣”挖掘儿童手脑潜能亲子培训营]]></Title>"
            + "<Description><![CDATA[“开启学习兴趣”挖掘儿童手脑潜能亲子培训营]]></Description>"
            + "<PicUrl><![CDATA[https://mmbiz.qlogo.cn/mmbiz/2x9sALwpIbXg4UswX9fFJSiaNdG2gWfdzsWIvpUqGk2x9iaYVD1bwiaibib66HmgE5Sa2OO5xscbtibRJIpy83eFXQ0A/0]]></PicUrl>"
            + "<Url><![CDATA[http://mp.weixin.qq.com/s?__biz=MzA3MTM1OTIwNg==&mid=201937614&idx=2&sn=43f78504c5e16e9903bdbcf2be1dc064#rd]]></Url>"
            + "</item>";
        return xmlStr;
    }

    public static XmlDocument CreateReplyDocument(int id)
    {

        XmlDocument xmlD = new XmlDocument();
        xmlD.LoadXml("<xml></xml>");

        string from = "";
        string to = "";
        string msgType = "";
        int msgCount = 1;
        string content = "";

        SqlConnection conn = new SqlConnection(System.Configuration.ConfigurationSettings.AppSettings["constr"].Trim());
        SqlCommand cmd = new SqlCommand(" select * from wxreplymsg where wxreplymsg_id = " + id.ToString(), conn);
        conn.Open();
        SqlDataReader sqlDr = cmd.ExecuteReader();
        if (sqlDr.Read())
        {
            msgType = sqlDr["wxreplymsg_msgtype"].ToString().Trim();
            from = sqlDr["wxreplymsg_from"].ToString().Trim();
            to = sqlDr["wxreplymsg_to"].ToString().Trim();
            msgCount = int.Parse(sqlDr["wxreplymsg_msgcount"].ToString().Trim());

            XmlNode n = xmlD.CreateNode(XmlNodeType.Element, "FromUserName", "");
            n.InnerXml = "<![CDATA[" + from + "]]>";
            xmlD.SelectSingleNode("//xml").AppendChild(n);

            n = xmlD.CreateNode(XmlNodeType.Element, "ToUserName", "");
            n.InnerXml = "<![CDATA[" + to + "]]>";
            xmlD.SelectSingleNode("//xml").AppendChild(n);

            n = xmlD.CreateNode(XmlNodeType.Element, "CreateTime", "");
            n.InnerText = GetTimeStamp().Trim();
            xmlD.SelectSingleNode("//xml").AppendChild(n);

            n = xmlD.CreateNode(XmlNodeType.Element, "MsgType", "");
            n.InnerXml = "<![CDATA[" + msgType + "]]>";
            xmlD.SelectSingleNode("//xml").AppendChild(n);



            switch (msgType)
            {
                case "text":
                    content = sqlDr["wxreplymsg_content"].ToString().Trim();
                    n = xmlD.CreateNode(XmlNodeType.Element, "Content", "");
                    n.InnerXml = "<![CDATA[" + content + "]]>";
                    xmlD.SelectSingleNode("//xml").AppendChild(n);

                    break;
                case "news":
                    n = xmlD.CreateNode(XmlNodeType.Element, "ArticleCount", "");
                    n.InnerXml = "<![CDATA[" + sqlDr["wxreplymsg_msgcount"].ToString().Trim() + "]]>";
                    xmlD.SelectSingleNode("//xml").AppendChild(n);
                    n = xmlD.CreateNode(XmlNodeType.Element, "Articles", "");
                    n.InnerXml = sqlDr["wxreplymsg_content"].ToString().Trim();
                    xmlD.SelectSingleNode("//xml").AppendChild(n);

                    break;
                case "image":
                    n = xmlD.CreateNode(XmlNodeType.Element, "Image", "");
                    XmlNode subN = xmlD.CreateNode(XmlNodeType.Element, "MediaId", "");
                    subN.InnerXml = "<![CDATA[" + sqlDr["wxreplymsg_content"].ToString().Trim() + "]]>";
                    n.AppendChild(subN);
                    xmlD.SelectSingleNode("//xml").AppendChild(n);
                    break;
                default:

                    break;
            }
        }

        sqlDr.Close();
        conn.Close();
        cmd.Dispose();
        conn.Dispose();

        return xmlD;
    }

    public static string GetTimeStamp()
    {
        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(ts.TotalSeconds).ToString();
    }

    public static string GetInviteCode(string openId)
    {
        DateTime nowDateTime = DateTime.Now;
        string nowStr = nowDateTime.Year.ToString() + nowDateTime.Month.ToString().PadLeft(2, '0') + nowDateTime.Day.ToString().PadLeft(2, '0')
            + nowDateTime.Hour.ToString().PadLeft(2, '0') + nowDateTime.Minute.ToString().PadLeft(2, '0') + nowDateTime.Second.ToString().PadLeft(2, '0')
            + nowDateTime.Millisecond.ToString().PadLeft(3, '0');
        SqlConnection conn = new SqlConnection(System.Configuration.ConfigurationSettings.AppSettings["constr"].Trim());
        SqlCommand cmd = new SqlCommand(" select top 1 qr_invite_list_code from qr_invite_list order by qr_invite_list_code desc ", conn);
        conn.Open();
        SqlDataReader dr = cmd.ExecuteReader();
        string qrCode = "0000000000000000000";
        if (dr.Read())
        {
            qrCode = dr.GetString(0);
        }
        dr.Close();
        int i = 0;
        if (nowStr.Trim().Equals(qrCode.Trim().Substring(0, 17)))
        { 
            i = int.Parse(qrCode.Substring(17,2));
            i++;
        }
        nowStr = nowStr+i.ToString().PadLeft(2,'0');
        cmd.CommandText = " insert into qr_invite_list (qr_invite_list_code , qr_invite_list_owner ) values ('" + nowStr.Trim() + "' , '"
            + openId.Trim().Replace("'", "") + "'  ) ";
        cmd.ExecuteNonQuery();
        cmd.CommandText = " select top 1 qr_invite_list_id from qr_invite_list order by qr_invite_list_id desc ";
        dr = cmd.ExecuteReader();
        long id = 0;
        if (dr.Read())
        {
            id = dr.GetInt64(0);
        }
        conn.Close();
        cmd.Dispose();
        conn.Dispose();
        return id.ToString();
    }

    public static int GetQrCode(string eventKey)
    {
        int qrCode = 0;
        if (eventKey.ToLower().StartsWith("qrscene_"))
        {
            eventKey = eventKey.Trim().Replace("qrscene_", "");
        }
        try
        {
            qrCode = int.Parse(eventKey);
        }
        catch
        { 
        
        }
        return qrCode;
    }

    public static string ConfirmQrCode(int qrCode, string newUserOpenId)
    {
        string originalUserOpenId = "";
        SqlConnection conn = new SqlConnection(System.Configuration.ConfigurationSettings.AppSettings["constr"].Trim());
        SqlCommand cmd = new SqlCommand(" select qr_invite_list_owner from qr_invite_list where qr_invite_list_id = " + qrCode.ToString(), conn);
        conn.Open();
        SqlDataReader drd = cmd.ExecuteReader();
        if (drd.Read())
        {
            originalUserOpenId = drd.GetString(0);
        }
        drd.Close();
        conn.Close();
        cmd.CommandText = " insert into qr_invite_list_detail ( qr_invite_list_detail_id , qr_invite_list_detail_openid ) "
            + " values ('" + qrCode.ToString() + "' , '" + newUserOpenId.Replace("'", "").Trim() + "' ) ";
        conn.Open();
        try
        {
            cmd.ExecuteNonQuery();
        }
        catch
        { 
        
        }
        conn.Close();
        return originalUserOpenId.Trim();
    }

    public static string GetUserInfoJsonStringByOpenid(string openId)
    {
        string token = Util.GetAccessToken(System.Configuration.ConfigurationSettings.AppSettings["wxappid"].Trim(),
            System.Configuration.ConfigurationSettings.AppSettings["wxappsecret"].Trim());
        HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://api.weixin.qq.com/cgi-bin/user/info?access_token="
            + token + "&openid=" + openId + "&lang=zh_CN");
        HttpWebResponse res = (HttpWebResponse)req.GetResponse();
        Stream s = res.GetResponseStream();
        StreamReader sdr = new StreamReader(s);
        string j = sdr.ReadToEnd();
        sdr.Close();
        s.Close();
        res.Close();
        req.Abort();
        return j;
    }

    public static string GetSimpleJsonValueByKey(string jsonStr, string key)
    {
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        Dictionary<string, object> json = (Dictionary<string, object>)serializer.DeserializeObject(jsonStr);
        object v;
        json.TryGetValue(key, out v);
        return v.ToString();
    }

    public static Dictionary<string, object> GetObjectFromJsonByKey(string jsonStr, string key)
    {
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        Dictionary<string, object> json = (Dictionary<string, object>)serializer.DeserializeObject(jsonStr);
        object v;
        json.TryGetValue(key, out v);
        return (Dictionary < string, object>)v;
    }

    public static Dictionary<string, object>[] GetObjectArrayFromJsonByKey(string jsonStr, string key)
    {
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        Dictionary<string, object> json = (Dictionary<string, object>)serializer.DeserializeObject(jsonStr);
        object v;
        json.TryGetValue(key, out v);
        object[] vArr = (object[])v;
        Dictionary<string, object>[] retArr = new Dictionary<string, object>[vArr.Length];
        for (int i = 0; i < retArr.Length; i++)
        {
            Dictionary<string, object> keyPairObjectArray = (Dictionary<string, object>)vArr[i];
            retArr[i] = keyPairObjectArray;
        }
        return retArr;
    }

    public static string GetSimpleJsonStringFromKeyPairArray(KeyValuePair<string, object>[] vArr)
    {
        string r = "";
        for (int i = 0; i < vArr.Length; i++)
        {
            if (i == 0)
            {
                r = "\"" + vArr[i].Key.Trim() + "\" : \"" + vArr[i].Value.ToString() + "\"";
            }
            else
            {
                r = r + ", \"" + vArr[i].Key.Trim() + "\" : \"" + vArr[i].Value.ToString() + "\" ";
            }
        }
        return "{ " + r + " }";
    }

    public static void DealLandingRequest(string openId)
    {
        //File.AppendAllText(Server.MapPath("log/login.txt"), DateTime.Now.ToString() + "  aaa\r\n");
        DateTime now = DateTime.Now;
        now = now.AddMinutes(-5);
        SqlDataAdapter da = new SqlDataAdapter(" select * from WxLoginRequest where WxLoginRequest_deal = 0 and wxloginrequest_crt > '" + now.ToString() + "' ",
            conStr);
        DataTable dt = new DataTable();
        if (dt.Rows.Count >= 0)
        {
            ////File.AppendAllText(Server.MapPath("log/login.txt"), DateTime.Now.ToString() + "  bbb\r\n");
            SqlConnection conn = new SqlConnection(conStr);
            SqlCommand cmd = new SqlCommand(" insert into WxLoginRequest (WxLoginRequest_openid ) values ('" + openId.Replace("'", "") + "' ) ", conn);
            conn.Open();
            int i = cmd.ExecuteNonQuery();
            //File.AppendAllText(Server.MapPath("log/login.txt"), DateTime.Now.ToString() + "  " + i.ToString().Trim() + "\r\n");
            conn.Close();
            cmd.Dispose();
            conn.Dispose();
        }
        dt.Dispose();
        da.Dispose();
    }

    public static byte[] GetBinaryFileContent(string filePathName)
    {
        FileStream fileStream = File.OpenRead(filePathName);
        byte[] bArr = new byte[fileStream.Length];
        for (long i = 0; i < fileStream.Length; i++)
        {
            bArr[i] = (byte)fileStream.ReadByte();
        }
        return bArr;
    }

    public static string GetHaojinMd5Sign(string KeyPairStringWillBeSigned, string key)
    {
        string str = GetSortedArrayString(KeyPairStringWillBeSigned);
        System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
        byte[] bArr = md5.ComputeHash(Encoding.UTF8.GetBytes(str + key.Trim()));
        string ret = "";
        foreach (byte b in bArr)
        {
            ret = ret + b.ToString("x").PadLeft(2, '0').ToUpper();
        }
        return ret;
    }

    public static string GetSortedArrayString(string str)
    {
        string[] strArr = str.Split('&');
        Array.Sort(strArr);
        return String.Join("&", strArr);
    }
}