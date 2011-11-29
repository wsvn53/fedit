/**
 * Fedit for Fiddler
 * @author:  ethan.wang, wsvn53@gmail.com
 * @version: 1.0.0
 * @date:    2011/11/29
 * 
 * This plugin required Fiddler version 2.1.1.3
 * 
 */

using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;
using Fiddler;
using System.IO;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;

[assembly: Fiddler.RequiredVersion("2.1.1.3")]

public class Fedit : IAutoTamper    // Ensure class is public, or Fiddler won't see it!
{
    private System.Windows.Forms.MenuItem miFedit;
    private Hashtable fileType;
    private Hashtable tmp_rules;
    private String fedit_path;
    private String fedit_rule_path;

    public Fedit()
    {
        this.miFedit = new System.Windows.Forms.MenuItem();
        this.miFedit.Text = "Edit";
        this.miFedit.Click += new System.EventHandler(this.OnFeditClick);

        // init HashTable
        fileType = new System.Collections.Hashtable();
        fileType.Add("text/html", ".htm");
        fileType.Add("text/plain", ".txt");
        fileType.Add("text/css", ".css");
        fileType.Add("application/javascript", ".js");
        fileType.Add("image/jpeg", ".jpg");
        fileType.Add("image/gif", ".gif");
        fileType.Add("image/png", ".png");

        tmp_rules = new System.Collections.Hashtable();

        fedit_path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) + "\\Fedit";
        fedit_rule_path = fedit_path + "\\rules";
    }

    public void OnLoad()
    {
        FiddlerApplication.Log.LogString("Fedit extension loaded.");
        FiddlerApplication.UI.mnuSessionContext.MenuItems.Add(0, this.miFedit);
        MenuItem miFeditSpit = new MenuItem("-");
        FiddlerApplication.UI.mnuSessionContext.MenuItems.Add(1, miFeditSpit);
        // Load rules from files
        if (File.Exists(fedit_rule_path + "\\rules.xml"))
        {
            FiddlerApplication.oAutoResponder.LoadRules(fedit_rule_path + "\\rules.xml");
        }
    }

    public void OnFeditClick(object sender, EventArgs e)
    {
        Session[] oSessions = FiddlerApplication.UI.GetSelectedSessions();
        foreach (Session oSession in oSessions)
        {
            String tmp_filetype = (String)fileType[oSession.oResponse.MIMEType.ToString()];
            String tmp_filename = System.Uri.EscapeDataString(oSession.fullUrl) + tmp_filetype;
            String fedit_file = fedit_path + "\\" + tmp_filename;
            FiddlerApplication.Log.LogString("Saving url \"" + oSession.fullUrl + "\" to temp file \"" + fedit_file + "\"");
            if (!Directory.Exists(fedit_path))
            {
                Directory.CreateDirectory(fedit_path);
            }
            if (!Directory.Exists(fedit_rule_path))
            {
                Directory.CreateDirectory(fedit_rule_path);
            }
            File.WriteAllBytes(fedit_file, oSession.responseBodyBytes);
            FiddlerApplication.Log.LogString(tmp_filename);
            // add AotuResponse rule
            if (tmp_rules.ContainsKey(oSession.fullUrl))
            {
                FiddlerApplication.oAutoResponder.RemoveRule((Fiddler.ResponderRule)tmp_rules[oSession.fullUrl]);
            }
            else
            {
                tmp_rules.Add(oSession.fullUrl, "");
            }
            tmp_rules[oSession.fullUrl] = (Fiddler.ResponderRule)FiddlerApplication.oAutoResponder.AddRule("EXACT:" + oSession.fullUrl, fedit_file, true);

            FiddlerApplication.Log.LogString("Open file \"" + fedit_file + "\"");
            // edit file with default editor
            System.Diagnostics.Process edit = new System.Diagnostics.Process();
            edit.StartInfo.FileName = fedit_file;
            edit.Start();
        }
    }

    public void OnBeforeUnload()
    {
        // clear all tmp rules and files
        foreach (DictionaryEntry item in tmp_rules)
        {
            FiddlerApplication.oAutoResponder.RemoveRule((Fiddler.ResponderRule)item.Value);
            File.Delete(((Fiddler.ResponderRule)item.Value).sAction);
        }
        // save rules, for next time onload restore
        FiddlerApplication.oAutoResponder.SaveRules(fedit_rule_path + "\\rules.xml");
    }

    public void AutoTamperRequestBefore(Session oSession) { }
    public void AutoTamperRequestAfter(Session oSession) { }
    public void AutoTamperResponseBefore(Session oSession) { }
    public void AutoTamperResponseAfter(Session oSession) { }
    public void OnBeforeReturningError(Session oSession) { }
}