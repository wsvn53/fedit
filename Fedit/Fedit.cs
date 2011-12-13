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
using System.Drawing;
using System.Text.RegularExpressions;

[assembly: Fiddler.RequiredVersion("2.1.1.3")]

public class Fedit : IAutoTamper    // Ensure class is public, or Fiddler won't see it!
{
    private System.Windows.Forms.MenuItem miFedit;
    private Hashtable fileType;
    private Hashtable tmp_rules;
    private Hashtable editor_setting;
    private String fedit_path;
    private String fedit_config_path;
    private ListView editor_list;
    private TextBox txt_filetype;
    private TextBox txt_editor;

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
    }

    public void OnLoad()
    {
        // add context menu item
        FiddlerApplication.Log.LogString("Fedit extension loaded.");
        FiddlerApplication.UI.mnuSessionContext.MenuItems.Add(0, this.miFedit);
        MenuItem miFeditSpit = new MenuItem("-");
        FiddlerApplication.UI.mnuSessionContext.MenuItems.Add(1, miFeditSpit);
        // add setting tabpage
        TabPage fedit_tab = new TabPage("Fedit");
        // add a group: Editor
        GroupBox gp_editor = new GroupBox();
        gp_editor.Text = "Editor Associate";
        gp_editor.Width = 500;
        gp_editor.Height = 255;
        gp_editor.Left = 10;
        gp_editor.Top = 10;
        // add a listview
        editor_list = new ListView();
        editor_list.Width = 480;
        editor_list.Height = 200;
        editor_list.Left = 10;
        editor_list.Top = 20;
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
                    FiddlerApplication.Log.LogString(file_type);
                    editor_setting[file_type] = editor_path;
                    FiddlerApplication.Log.LogString(file_type);
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
        txt_filetype.Left = 10;
        txt_filetype.Top = 225;
        gp_editor.Controls.Add(txt_filetype);
        txt_editor = new TextBox();
        txt_editor.Width = 295;
        txt_editor.Top = 225;
        txt_editor.Left = 112;
        gp_editor.Controls.Add(txt_editor);
        Button btn_add = new Button();
        btn_add.Text = "Add/Mod";
        btn_add.Width = 80;
        btn_add.Height = 22;
        btn_add.Top = 225;
        btn_add.Left = 410;
        btn_add.Click += new EventHandler(this.OnAddEditorItem);
        gp_editor.Controls.Add(btn_add);

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
            // check 304
            FiddlerApplication.Log.LogString(oSession.responseCode.ToString());
            if (oSession.responseCode.ToString() == "304") {
                if (MessageBox.Show("This request session's ResponseCode is 304, means no data responded!\nContinue to Edit?", "Fedit Warning:",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)  return;
            }

            String tmp_filetype;
            if (fileType.ContainsKey(oSession.oResponse.MIMEType.ToString()))
            {
                tmp_filetype = (String)fileType[oSession.oResponse.MIMEType.ToString()];
            }
            else
            {
                Stack<string> ext = new Stack<string>(oSession.fullUrl.Split(new string[]{ "." }, StringSplitOptions.None));
                tmp_filetype = "." + Regex.Replace(ext.Pop(), @"([a-zA-Z\d]+?)[^a-zA-Z\d].+", "$1");
            }
            String escaped_url = System.Uri.EscapeDataString(oSession.fullUrl);
            String tmp_filename = escaped_url.Substring(0, escaped_url.Length<150?escaped_url.Length:150) + tmp_filetype;
            String fedit_file = fedit_path + "\\" + tmp_filename;
            FiddlerApplication.Log.LogString("Saving url \"" + oSession.fullUrl + "\" to temp file \"" + fedit_file + "\"");
            if (oSession.oResponse.headers.ToString().ToLower().IndexOf("content-encoding:")>=0)
            {
                // if encoded, decode it
                File.WriteAllBytes(fedit_file, System.Text.Encoding.Default.GetBytes(oSession.GetResponseBodyAsString()));
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
            tmp_rules[oSession.fullUrl] = (Fiddler.ResponderRule)FiddlerApplication.oAutoResponder.AddRule("EXACT:" + oSession.fullUrl, fedit_file, true);

            FiddlerApplication.Log.LogString("Open file \"" + fedit_file + "\"");
            // edit file with default editor
            if (editor_setting.ContainsKey(tmp_filetype))
            {
                System.Diagnostics.Process.Start(editor_setting[tmp_filetype].ToString(), "\""+fedit_file+"\"");
            }
            else
            {
                System.Diagnostics.Process.Start(editor_setting["default"].ToString(), "\"" + fedit_file + "\"");
            }
            FiddlerApplication.Log.LogString(fedit_file);
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
        FiddlerApplication.oAutoResponder.SaveRules(fedit_config_path + "\\rules.xml");
    }

    public void AutoTamperRequestBefore(Session oSession) { }
    public void AutoTamperRequestAfter(Session oSession) { }
    public void AutoTamperResponseBefore(Session oSession) { }
    public void AutoTamperResponseAfter(Session oSession) { }
    public void OnBeforeReturningError(Session oSession) { }
}