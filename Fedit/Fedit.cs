/**
 * Fedit for Fiddler
 * @author:  ethan.wang, wsvn53@gmail.com
 * @version: 2.0.0
 * @date:    2011/11/29
 * @update : 2013/09/01
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
using System.Drawing;
using System.Text.RegularExpressions;

[assembly: Fiddler.RequiredVersion("2.1.1.3")]

public class Fedit : IAutoTamper    // Ensure class is public, or Fiddler won't see it!
{
    private System.Windows.Forms.MenuItem miFedit;
    private System.Windows.Forms.MenuItem miFeditWith;
    private Hashtable fileType;
    private Hashtable tmp_rules;
    private Hashtable editor_setting;
    private String fedit_path;
    private String fedit_config_path;
    private ListView editor_list;
    private TextBox txt_filetype;
    private TextBox txt_editor;
    private OpenFileDialog file_editor;
    private String urlLeft;     // stored url left part without query params.
    private Form paramsForm;
    private CheckedListBox paramList;
    private Session editWithSession;
    private CheckBox chkJsonp;

    public Fedit()
    {
        this.miFedit = new System.Windows.Forms.MenuItem();
        this.miFedit.Text = "Edit";
        this.miFedit.Click += new System.EventHandler(this.OnFeditClick);

        this.miFeditWith = new System.Windows.Forms.MenuItem();
        this.miFeditWith.Text = "Edit with Parameters..";
        this.miFeditWith.Click += new System.EventHandler(this.OnFeditWithClick);

        // init HashTable
        fileType = new System.Collections.Hashtable();
        fileType.Add("text/html", ".htm");
        fileType.Add("text/plain", ".txt");
        fileType.Add("text/css", ".css");
        fileType.Add("application/javascript", ".js");
        fileType.Add("image/jpeg", ".jpg");
        fileType.Add("image/gif", ".gif");
        fileType.Add("image/png", ".png");

        tmp_rules = new Hashtable();

        fedit_path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) + "\\Fedit";
        fedit_config_path = fedit_path + "\\config";

        if (!Directory.Exists(fedit_path))
        {
            Directory.CreateDirectory(fedit_path);
        }
        if (!Directory.Exists(fedit_config_path))
        {
            Directory.CreateDirectory(fedit_config_path);
        }

        editor_setting = new Hashtable();
        editor_setting.Add("default", "notepad");
        editor_setting.Add(".css", "notepad");
        editor_setting.Add(".js", "notepad");
        editor_setting.Add(".jpg", "mspaint");
        editor_setting.Add(".png", "mspaint");
        editor_setting.Add(".gif", "mspaint");
    }

    public void OnLoad()
    {
        // add context menu item
        FiddlerApplication.Log.LogString("Fedit extension loaded.");
        FiddlerApplication.UI.mnuSessionContext.MenuItems.Add(0, this.miFedit);
        FiddlerApplication.UI.mnuSessionContext.MenuItems.Add(1, this.miFeditWith);
        MenuItem miFeditSpit = new MenuItem("-");
        FiddlerApplication.UI.mnuSessionContext.MenuItems.Add(2, miFeditSpit);
        // add response process function, for jsonp request replacement.
        Fiddler.FiddlerApplication.BeforeResponse += delegate(Fiddler.Session oS)
        {
            // find which rule match this url.
            foreach (Fiddler.ResponderRule rule in tmp_rules.Values)
            {
                bool ruleDisable = false;
                System.Reflection.PropertyInfo p = rule.GetType().GetProperty("bDisableOnMatch", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if(p!=null)
                {
                    // FiddlerApplication.Log.LogString("Using rule.bDisableOnMatch.");
                    // some version of fidller2 maybe have no bDisableOnMatch.
                    ruleDisable = (bool)p.GetValue(rule, null);
                }
                if (!ruleDisable&& rule.sMatch.IndexOf("regex:") > -1 && rule.sMatch.IndexOf("#jsonp:") > -1)
                {
                    String[] regexSplit = Regex.Split(rule.sMatch, "regex:");
                    Regex regex = new Regex(regexSplit[1]);
                    if (regex.IsMatch(oS.fullUrl))
                    {
                        FiddlerApplication.Log.LogString("Matched JSONP rule: " + oS.fullUrl);
                        String[] jsonpInfo = Regex.Split(rule.sMatch, "#jsonp:")[1].Split('=');
                        String replaceKey = jsonpInfo[0];
                        String replaceValue = jsonpInfo[1];
                        Regex toReg = new Regex(replaceKey + "=([^&]+)");
                        Match toMatch = toReg.Match(oS.fullUrl);
                        String replaceTo = toMatch.Groups[1].Value;
                        FiddlerApplication.Log.LogString("Will replace '"+ replaceValue +"' to '" + replaceTo + "'..");
                        oS.utilReplaceInResponse(replaceValue, replaceTo);
                        break;
                    }
                }
            }
        };
        // add setting tabpage
        TabPage fedit_tab = new TabPage("Fedit");
        // add a group: Editor
        GroupBox gp_editor = new GroupBox();
        gp_editor.Text = "Editor Associate";
        gp_editor.Width = 500;
        gp_editor.Height = 300;
        gp_editor.Location = new Point(10, 10);
        // add a listview
        editor_list = new ListView();
        editor_list.Width = 480;
        editor_list.Height = 200;
        editor_list.Location = new Point(10, 20);
        editor_list.GridLines = true;
        editor_list.FullRowSelect = true;
        editor_list.View = View.Details;
        editor_list.Scrollable = true;
        editor_list.MultiSelect = true;
        MenuItem delete_eidtor = new MenuItem();
        delete_eidtor.Text = "Delete";
        delete_eidtor.Click += new EventHandler(this.OnDeleteEditorItem);
        ContextMenu editor_menu = new ContextMenu();
        editor_menu.MenuItems.Add(0, delete_eidtor);
        editor_list.ContextMenu = editor_menu;
        editor_list.Columns.Add("Filetype", 100, HorizontalAlignment.Left);
        editor_list.Columns.Add("Open in Editor", 370, HorizontalAlignment.Left);
        // bind click event
        // editor_list.Click += new EventHandler(this.OnClickEditorItem);
        // load Editor setting from editor.cfg
        if (File.Exists(fedit_config_path + "\\editor.cfg"))
        {
            String[] editor_string = File.ReadAllLines(fedit_config_path + "\\editor.cfg");
            for (int i = 0, len = editor_string.Length; i < len; i++)
            {
                if (editor_string[i] == "") continue;
                Queue<string> editor = new Queue<string>(editor_string[i].Split(new string[] { " " }, StringSplitOptions.None));
                String file_type = editor.Dequeue();
                String editor_path = String.Join(" ", editor.ToArray());
                if (editor_setting.ContainsKey(file_type))
                {
                    editor_setting[file_type] = editor_path;
                }
                else
                {
                    editor_setting.Add(file_type, editor_path);
                }
            }
        }
        // add to list view
        foreach (DictionaryEntry item in editor_setting)
        {
            ListViewItem li = new ListViewItem();
            li.SubItems.Clear();
            li.SubItems[0].Text = item.Key.ToString();
            li.SubItems.Add(item.Value.ToString());
            editor_list.Items.Add(li);
        }

        gp_editor.Controls.Add(editor_list);
        // add input box
        txt_filetype = new TextBox();
        txt_filetype.Width = 100;
        txt_filetype.Location = new Point(10, 225);
        gp_editor.Controls.Add(txt_filetype);
        txt_editor = new TextBox();
        txt_editor.Width = 270;
        txt_editor.Location = new Point(112, 225);
        gp_editor.Controls.Add(txt_editor);
        Button btn_browse = new Button();
        btn_browse.Text = "..";
        btn_browse.Width = 22;
        btn_browse.Height = 22;
        btn_browse.Location = new Point(385, 225);
        btn_browse.Click += new EventHandler(this.OnBrowseFile);
        gp_editor.Controls.Add(btn_browse);
        file_editor = new OpenFileDialog();
        Button btn_add = new Button();
        btn_add.Text = "Add/Mod";
        btn_add.Width = 80;
        btn_add.Height = 22;
        btn_add.Location = new Point(410, 225);
        btn_add.Click += new EventHandler(this.OnAddEditorItem);
        gp_editor.Controls.Add(btn_add);
        // copyright
        Label copyRight = new Label();
        copyRight.Width = 400;
        copyRight.Height = 18;
        copyRight.Location = new Point(10, 258);
        copyRight.Text = "Developed by Ethan(http://imethan.com/).";
        gp_editor.Controls.Add(copyRight);
        // source code
        Label lblSrc = new Label();
        lblSrc.Width = 400;
        lblSrc.Height = 18;
        lblSrc.Location = new Point(10, 276);
        lblSrc.Text = "Source code: https://github.com/wsvn53/fedit.";
        gp_editor.Controls.Add(lblSrc);
        
        fedit_tab.Controls.Add(gp_editor);
        FiddlerApplication.UI.tabsViews.TabPages.Add(fedit_tab);
        // Load rules from files
        if (File.Exists(fedit_config_path + "\\rules.xml"))
        {
            FiddlerApplication.oAutoResponder.LoadRules(fedit_config_path + "\\rules.xml");
        }
    }

    public void OnDeleteEditorItem(object sender, EventArgs e)
    {
        foreach (System.Windows.Forms.ListViewItem eachItem in editor_list.SelectedItems)
        {
            editor_list.Items.Remove(eachItem);
            editor_setting.Remove(eachItem.SubItems[0].Text);
        }
        this.SaveEditorSetting();
    }

    public void OnBrowseFile(object sender, EventArgs e)
    {
        if (file_editor.ShowDialog() == DialogResult.Cancel) return;
        try
        {
            txt_editor.Text = file_editor.FileName;
        }
        catch (Exception)
        {
            MessageBox.Show("Error opening file", "File Error",
            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
    }

    public void OnAddEditorItem(object sender, EventArgs e)
    {
        if (txt_filetype.Text == "" || txt_editor.Text == "")
        {
            MessageBox.Show("File type and editor path fields can not be empty.");
            return;
        }
        // add to HashTable
        if (editor_setting.ContainsKey(txt_filetype.Text))
        {
            editor_setting[txt_filetype.Text] = txt_editor.Text;
            foreach (System.Windows.Forms.ListViewItem eachItem in editor_list.Items)
            {
                if (eachItem.SubItems[0].Text == txt_filetype.Text)
                {
                    eachItem.SubItems[1].Text = txt_editor.Text;
                    break;
                }
            }
        }
        else
        {
            editor_setting.Add(txt_filetype.Text, txt_editor.Text);
            // add to listview
            ListViewItem li = new ListViewItem();
            li.SubItems.Clear();
            li.SubItems[0].Text = txt_filetype.Text;
            li.SubItems.Add(txt_editor.Text);
            editor_list.Items.Add(li);
            txt_filetype.Text = "";
            txt_editor.Text = "";
        }
        this.SaveEditorSetting();
    }

    private void SaveEditorSetting() {
        String setting_string = "";
        foreach (DictionaryEntry item in editor_setting)
        {
            setting_string += (item.Key.ToString() + " " + item.Value.ToString() + "\n");
        }
        File.WriteAllText(fedit_config_path + "\\editor.cfg", setting_string.TrimEnd("\n".ToCharArray()));
    }

    public void OnFeditClick(object sender, EventArgs e)
    {
        Session[] oSessions = FiddlerApplication.UI.GetSelectedSessions();
        // enable AutoResponder
        FiddlerApplication.oAutoResponder.IsEnabled = true;
        FiddlerApplication.oAutoResponder.PermitFallthrough = true;
        foreach (Session oSession in oSessions)
        {
            this.processRuleForSession(oSession, "");
        }
    }

    private void processRuleForSession(Session oSession, String ruleStr)
    {
        oSession.utilDecodeResponse();
        // check 304
        if (oSession.responseCode.ToString() == "304")
        {
            if (MessageBox.Show("This request session's ResponseCode is 304, means no data responded!\nContinue to Edit?", "Fedit Warning:",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No) return;
        }

        // check encode type
        FiddlerApplication.Log.LogString(oSession.GetResponseBodyEncoding().ToString());

        String tmp_filetype;
        if (fileType.ContainsKey(oSession.oResponse.MIMEType.ToString()))
        {
            tmp_filetype = (String)fileType[oSession.oResponse.MIMEType.ToString()];
        }
        else
        {
            Stack<string> ext = new Stack<string>(oSession.fullUrl.Split(new string[] { "." }, StringSplitOptions.None));
            tmp_filetype = "." + Regex.Replace(ext.Pop(), @"([a-zA-Z\d]+?)[^a-zA-Z\d].+", "$1");
        }
        String escaped_url = System.Uri.EscapeDataString(oSession.fullUrl);
        String tmp_filename = escaped_url.Substring(0, escaped_url.Length < 150 ? escaped_url.Length : 150) + tmp_filetype;
        String fedit_file = fedit_path + "\\" + tmp_filename;
        FiddlerApplication.Log.LogString("Saving url \"" + oSession.fullUrl + "\" to temp file \"" + fedit_file + "\"");
        if (oSession.oResponse.headers.ToString().ToLower().IndexOf("content-encoding:") >= 0)
        {
            // if encoded, decode it
            File.WriteAllBytes(fedit_file, oSession.responseBodyBytes);
        }
        else
        {
            // else load bytes
            File.WriteAllBytes(fedit_file, oSession.responseBodyBytes);
        }
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
        tmp_rules[oSession.fullUrl] = (Fiddler.ResponderRule)FiddlerApplication.oAutoResponder.AddRule((ruleStr==""||ruleStr==null)?"EXACT:" + oSession.fullUrl:ruleStr, fedit_file, true);
        if(!FiddlerApplication.oAutoResponder.IsEnabled)
            MessageBox.Show("Notice! AutoResponder is not enabled! Please enable this.");

        FiddlerApplication.Log.LogString("Open file \"" + fedit_file + "\"");
        // edit file with default editor
        if (editor_setting.ContainsKey(tmp_filetype))
        {
            System.Diagnostics.Process.Start(editor_setting[tmp_filetype].ToString(), "\"" + fedit_file + "\"");
        }
        else
        {
            System.Diagnostics.Process.Start(editor_setting["default"].ToString(), "\"" + fedit_file + "\"");
        }
        FiddlerApplication.Log.LogString(fedit_file);
    }

    public void OnFeditWithClick(object sender, EventArgs e)
    {
        Session[] oSessions = FiddlerApplication.UI.GetSelectedSessions();
       foreach (Session oSession in oSessions)
       {
            String[] url = oSession.fullUrl.Split('?');
            urlLeft = "";
            if (url.Length > 1)
            {
                String[] urlParams = url[url.Length - 1].Split('&');
                String[] urlLeftArr = new String[url.Length - 1];
                Array.Copy(url, 0, urlLeftArr, 0, url.Length - 1);
                urlLeft = String.Join("?", urlLeftArr);
                FiddlerApplication.Log.LogString("Left: " + urlLeft.ToString());
                FiddlerApplication.Log.LogString("Parameters: " + String.Join("&", urlParams).ToString());
                editWithSession = oSession;
                this.showEditParams(urlParams);
            }
            else
            {
                MessageBox.Show("No query parameters found in the following url, please 'Edit' directly. \n" + oSession.fullUrl);
            }
        }
    }

    private void showEditParams(String[] urlParams)
    {
        paramsForm = new Form();
        paramsForm.Width = 560;
        paramsForm.Height = 440;
        paramsForm.Text = "Please select parameters to edit..";
        paramsForm.FormBorderStyle = FormBorderStyle.FixedDialog;
        paramsForm.StartPosition = FormStartPosition.CenterParent;
        paramsForm.MinimizeBox = false;
        paramsForm.MaximizeBox = false;

        // add url label
        TextBox txtUrl = new TextBox();
        txtUrl.ReadOnly = true;
        txtUrl.Location = new Point(20, 10);
        txtUrl.Width = paramsForm.Width - 40;
        txtUrl.Text = editWithSession.fullUrl;
        paramsForm.Controls.Add(txtUrl);

        // jsonp type option
        chkJsonp = new CheckBox();
        chkJsonp.Text = "Is this a JSONP request? I'll replace response body with the value you selected.";
        chkJsonp.Location = new Point(20, 40);
        chkJsonp.Width = paramsForm.Width - 40;
        chkJsonp.BackColor = Color.DarkOrange;
        paramsForm.Controls.Add(chkJsonp);

        // add params ui
        paramList = new CheckedListBox();
        paramList.Location = new Point(20, 70);
        paramList.Width = paramsForm.Width - 40;
        paramList.Height = 300;
        paramList.Items.AddRange(urlParams);
        //paramList.CheckOnClick = true;

        for (int i = 0; i < paramList.Items.Count; i++)
        {
            paramList.SetItemChecked(i, true);
        }

        paramsForm.Controls.Add(paramList);

        // add "Edit it!" button
        Button btn_go = new Button();
        btn_go.Text = "Edit it!";
        btn_go.Width = 250;
        btn_go.Height = 22;
        btn_go.Location = new Point(paramsForm.Width / 2 - btn_go.Width / 2, 370);
        btn_go.Click += new EventHandler(this.OnFeditParamsWithClick);
        paramsForm.Controls.Add(btn_go);

        paramsForm.AcceptButton = btn_go;
        paramList.Focus();

        paramsForm.ShowDialog();
    }

    public void OnFeditParamsWithClick(object sender, EventArgs e)
    {
        String ruleStr = "regex:(?insx)^" + urlLeft.Replace("?", "\\?").Replace(".", "\\.").Replace("-", "\\-") + "\\?";
        for (int i = 0; i < paramList.Items.Count;i++ )
        {
            if (paramList.GetItemChecked(i))
            {
                ruleStr += paramList.Items[i].ToString().Replace("?", "\\?").Replace(".", "\\.").Replace("-", "\\-");
            }
            else
            {
                // use regexp to ignore this parameter
                ruleStr += "[^&]+";
            }
            if (i < paramList.Items.Count - 1)
            {
                // add "&"
                ruleStr += "&";
            }
        }

        ruleStr += "$";

        // check jsonp request
        if (chkJsonp.Checked&&paramList.SelectedItems.Count>0)
        {
            if (!paramList.GetItemChecked(paramList.SelectedIndex))
            {
                //String[] sel = paramList.SelectedItem.ToString().Split('=');
                ruleStr += " #jsonp:" + paramList.SelectedItem.ToString();
            }
            else
            {
                MessageBox.Show("JSONP callback parameter must be unchecked!", "Notice:", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        FiddlerApplication.Log.LogString("Rule: " + ruleStr);
        this.processRuleForSession(editWithSession, ruleStr);
        paramsForm.Close();
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
        FiddlerApplication.oAutoResponder.SaveRules(fedit_config_path + "\\rules.xml");
    }

    public void AutoTamperRequestBefore(Session oSession) { }
    public void AutoTamperRequestAfter(Session oSession) { }
    public void AutoTamperResponseBefore(Session oSession) { }
    public void AutoTamperResponseAfter(Session oSession) { }
    public void OnBeforeReturningError(Session oSession) { }
}