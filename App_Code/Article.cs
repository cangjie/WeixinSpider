using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;

/// <summary>
/// Summary description for Article
/// </summary>
public class Article
{
    public int id = 0;
    public string title = "";
    public string urlSuffix = "";
    public string accountName = "";

    public Article()
    {
        //
        // TODO: Add constructor logic here
        //
    }

    public Article(int articleId)
    {
        DataTable dt = DBHelper.GetDataTable(" select * from weixin_article where [id] = " + articleId);
        if (dt.Rows.Count == 1)
        {
            id = int.Parse(dt.Rows[0]["id"].ToString());
            title = dt.Rows[0]["title"].ToString().Trim();
            urlSuffix = dt.Rows[0]["url_suffix"].ToString().Trim();
            accountName = dt.Rows[0]["account_name"].ToString().Trim();
        }
        dt.Dispose();
    }

    public void UpdateReadNum()
    {
        if (id == 0)
            return;
        string jsonStr = Util.GetWebContent("http://mp.weixin.qq.com/mp/getcomment?" + urlSuffix.Trim());
        int readNum = 0;
        try
        {
            readNum = int.Parse(Util.GetSimpleJsonValueByKey(jsonStr, "read_num").Trim());
        }
        catch
        {

        }
        if (readNum>0)
        {
            string[,] updateParam = { { "read_num", "int", readNum.ToString() }, { "upd", "datetime", DateTime.Now.ToString() } };
            string[,] keyParam = { { "id", "int", id.ToString() } };
            DBHelper.UpdateData("weixin_article", updateParam, keyParam, Util.conStr);
            string[,] insertParam = { { "article_id", "int", id.ToString() }, { "read_num", "int", readNum.ToString() } };
            DBHelper.InsertData("article_read_num", insertParam);
        }
        
    }

    public static Article TryAddArticle(string suffix, string title, string accountName)
    {
        DataTable dt = DBHelper.GetDataTable(" select * from weixin_article where url_suffix = '" + suffix + "' and title = '" + title + "' and account_name = '" + accountName.Trim() + "'  ");
        if (dt.Rows.Count == 0)
        {
            string[,] insertParam = { { "title", "varchar", title.Trim() }, 
                { "url_suffix", "varchar", suffix.Trim() }, {"account_name", "varchar", accountName.Trim() } };
            int i = DBHelper.InsertData("weixin_article", insertParam);
            if (i == 1)
            {
                DataTable dtNewRow = DBHelper.GetDataTable(" select max([id]) from weixin_article ");
                int newId = int.Parse(dtNewRow.Rows[0][0].ToString());
                Article article =  new Article(newId);
                article.UpdateReadNum();
                return article;
            }
            else
            {
                return null;
            }
        }
        else
        {
            Article article = new Article(int.Parse(dt.Rows[0]["id"].ToString()));
            return article;
        }
    }

    public static Article[] GetRecentArticle()
    {
        DataTable dt = DBHelper.GetDataTable(" select * from weixin_article ");
        Article[] articleArr = new Article[dt.Rows.Count];
        for (int i = 0; i < dt.Rows.Count; i++)
        {
            articleArr[i] = new Article();
            articleArr[i].id = int.Parse(dt.Rows[i]["id"].ToString());
            articleArr[i].title = dt.Rows[i]["title"].ToString().Trim();
            articleArr[i].urlSuffix = dt.Rows[i]["url_suffix"].ToString().Trim();
            articleArr[i].accountName = dt.Rows[i]["account_name"].ToString().Trim();
        }
        return articleArr;
    }
}