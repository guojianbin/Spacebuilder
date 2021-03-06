﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;

/// <summary>
/// MCrawler 的摘要说明
/// </summary>
public class MCrawlerHandler : MHandler
{
    private string[] Sources;
    private MCrawler[] MCrawlers;
    public MCrawlerHandler(HttpContext context) : base(context) { }

    public override void Process()
    {
        Sources = Request.Form.GetValues("source[]");
        if (Sources == null || Sources.Length == 0)
        {
            WriteJson(new
            {
                state = "参数错误：没有指定抓取源"
            });
            return;
        }
        MCrawlers = Sources.Select(x => new MCrawler(x, Server).Fetch()).ToArray();
        WriteJson(new
        {
            state = "SUCCESS",
            list = MCrawlers.Select(x => new
            {
                state = x.State,
                source = x.SourceUrl,
                url = x.ServerUrl
            })
        });
    }
}

public class MCrawler
{
    public string SourceUrl { get; set; }
    public string ServerUrl { get; set; }
    public string State { get; set; }

    private HttpServerUtility Server { get; set; }


    public MCrawler(string sourceUrl, HttpServerUtility server)
    {
        this.SourceUrl = sourceUrl;
        this.Server = server;
    }

    public MCrawler Fetch()
    {
        var request = HttpWebRequest.Create(this.SourceUrl) as HttpWebRequest;
        using (var response = request.GetResponse() as HttpWebResponse)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                State = "Url returns " + response.StatusCode + ", " + response.StatusDescription;
                return this;
            }
            if (response.ContentType.IndexOf("image") == -1)
            {
                State = "Url is not an image";
                return this;
            }
            ServerUrl = Tunynet.Utilities.WebUtility.ResolveUrl(PathFormatter.Format(Path.GetFileName(this.SourceUrl), Config.GetString("catcherPathFormat")));
            var savePath = Tunynet.Utilities.WebUtility.GetPhysicalFilePath(ServerUrl);
            if (!Directory.Exists(Path.GetDirectoryName(savePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
            }
            try
            {
                var stream = response.GetResponseStream();
                var reader = new BinaryReader(stream);
                byte[] bytes;
                using (var ms = new MemoryStream())
                {
                    byte[] buffer = new byte[4096];
                    int count;
                    while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        ms.Write(buffer, 0, count);
                    }
                    bytes = ms.ToArray();
                }
                File.WriteAllBytes(savePath, bytes);
                State = "SUCCESS";
            }
            catch (Exception e)
            {
                State = "抓取错误：" + e.Message;
            }
            return this;
        }
    }
}