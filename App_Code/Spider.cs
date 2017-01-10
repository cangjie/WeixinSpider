using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using System.Threading;

/// <summary>
/// Summary description for Spider
/// </summary>
public class Spider
{
    public static Thread threadCollectArticles;
    public static Thread threadUpdateArticleReadNum;

    public Spider()
    {
        //
        // TODO: Add constructor logic here
        //
    }

    public static void AnalizeArticle(string url)
    {
        string listPageContent = Util.GetWebContent(url);
        string regBlockStr = "<li +d=\"[^\"]+\" *>[\\s|\\S]+?</li>";
        string regAnchorStr = "<a uigs=\"pc_\\d+_\\d+_title\" href=\"http://mp.weixin.qq.com/s?[^\"]+\" target=\"_blank\">.+</a>";
        string regAccountStr = "data-isV=\"[0,1]\">.+</a>";
        string regUrlStr = "http://mp.weixin.qq.com/s?[^\"]+";
        string regTitleStr = ">.+?<";
        MatchCollection matches = Regex.Matches(listPageContent, regBlockStr);
        foreach (Match match in matches)
        {
            string blockStr = match.Value.Trim();
            string anchor = Regex.Match(blockStr, regAnchorStr).Value;
            string account = Regex.Match(blockStr, regAccountStr).Value;
            string articleUrl = Regex.Match(anchor, regUrlStr).Value;
            string articleTitle = Regex.Match(anchor, regTitleStr).Value;
            account = Regex.Match(account, regTitleStr).Value;
            articleTitle = articleTitle.Replace(">", "").Replace("<", "").Trim();
            account = account.Replace(">", "").Replace("<", "").Trim();
            Article.TryAddArticle(articleUrl.Split('?')[1].Trim(), articleTitle, account);
        }
    }

    public static void CollectArticles()
    {
        for(;true;)
        {
            AnalizeArticle("http://weixin.sogou.com/pcindex/pc/pc_11/pc_11.html");
            AnalizeArticle("http://weixin.sogou.com/pcindex/pc/pc_17/pc_17.html");
            Thread.Sleep(900000);
        }
        
    }

    public static void UpdateArticleReadNum()
    {
        for (; true;)
        {
            Article[] articleArr = Article.GetRecentArticle();
            foreach (Article article in articleArr)
            {
                article.UpdateReadNum();
            }
            Thread.Sleep(900000);
        }
    }

    
}