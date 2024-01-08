﻿using Sunny.UI;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using WHC.OrderWater.Commons;

namespace WindowsBaselineAssistant
{
    public partial class Main : UIForm
    {
        public Main()
        {
            InitializeComponent();
        }
        XmlNodeList xmlNodeList;
        XmlDocument xmlDocument;
        XmlElement xmlElement;
        static string secInfoFile = Util.CurrentDirectory + "config.cfg";
        static string fortifyFile = Util.CurrentDirectory + "fortify.cfg";
        string secInfoCmd = "secedit /export /cfg config.cfg";
        string fortifyCmd = "secedit /configure /db fortify.sdb /cfg fortify.cfg";
        string openRegCmd = "regjump.exe {0} /accepteula";

        private void SetPassStyle(int index)
        {
            BaselineList.Rows[index].Cells[6].Style.ForeColor = Color.Green;
            BaselineList.Rows[index].Cells[7].ReadOnly = true;
            BaselineList.Rows[index].Cells[7].Style.BackColor = Color.LightGray;
        }

        private void AboutLinkLabel_Click(object sender, EventArgs e)
        {
            Process.Start("https://saferoad.cc/");
        }

        private void CheckBtn_Click(object sender, EventArgs e)
        {
            try
            {
                xmlDocument = Util.ReadXml(Util.CurrentDirectory + @"\item.xml");
                if (xmlDocument == null)
                {
                    UIMessageBox.ShowError("未找到配置文件");
                    return;
                }
                if (File.Exists(secInfoFile))
                {
                    File.Delete(secInfoFile);
                }
                //int code = GenerateSecInfo();
                if (!Util.ExecutExternalProgram(secInfoCmd, true, ProcessWindowStyle.Hidden).Equals(0))
                {
                    UIMessageBox.ShowError("secedit信息生成失败");
                    return;
                }
                BaselineList.Rows.Clear();
                int totalCount, passCount = 0;
                double rate, ratePercent = 0;
                xmlElement = xmlDocument.DocumentElement;
                xmlNodeList = xmlElement.ChildNodes;
                foreach (XmlNode xmlNode in xmlNodeList)
                {
                    int index = BaselineList.Rows.Add();
                    /*string name = xmlNode["name"].InnerText;*/
                    BaselineList.Rows[index].Cells[0].Value = xmlNode["name"].InnerText;
                    BaselineList.Rows[index].Cells[1].Value = xmlNode["description"].InnerText;
                    BaselineList.Rows[index].Cells[4].Value = xmlNode["standard"].InnerText;
                    string queryType = xmlNode["type"].InnerText;
                    string reality = string.Empty, section = string.Empty, warning = string.Empty;
                    //string warning = string.Empty;
                    string standard = xmlNode["standard"].InnerText;
                    switch (queryType)
                    {
                        case "secedit":
                            BaselineList.Rows[index].Cells[2].Value = "-";
                            BaselineList.Rows[index].Cells[3].Value = "-";
                            string mark = xmlNode["mark"].InnerText;
                            (section, reality) = Util.GetResultByMark(mark);
                            BaselineList.Rows[index].Cells[2].Value = section;
                            BaselineList.Rows[index].Cells[3].Value = mark;
                            BaselineList.Rows[index].Cells[5].Value = reality;
                            break;
                        case "registry":
                            BaselineList.Rows[index].Cells[2].Value = xmlNode["registry"].InnerText;
                            BaselineList.Rows[index].Cells[3].Value = xmlNode["regitem"].InnerText;
                            reality = RegistryHelper.GetValue(xmlNode["registry"].InnerText, xmlNode["regitem"].InnerText);
                            BaselineList.Rows[index].Cells[5].Value = reality;
                            break;
                        default:
                            break;
                    }
                    string dataType = xmlNode["dtype"].InnerText;
                    if (Util.ContainsElement(xmlNode, "warning"))
                    {
                        BaselineList.Rows[index].Cells[6].Value = "手动加固";
                        BaselineList.Rows[index].Cells[7].ReadOnly = true;
                        BaselineList.Rows[index].Cells[7].Style.BackColor = Color.Orange;
                        BaselineList.Rows[index].Cells[7].ToolTipText = "该项不支持自动加固";
                        passCount++;
                        continue;
                    }
                    BaselineList.Rows[index].Cells[6].Value = "不符合";
                    if (reality.Equals("未设置"))
                    {
                        continue;
                    }
                    switch (dataType)
                    {
                        case "fixed"://注：固定值为不符合
                            if (!reality.Equals(standard))
                            {
                                SetPassStyle(index);
                                BaselineList.Rows[index].Cells[6].Value = "符合";
                                passCount++;
                            }
                            break;
                        case "equals":
                        case "enum":
                            if (reality.Equals(standard))
                            {
                                SetPassStyle(index);
                                BaselineList.Rows[index].Cells[6].Value = "符合";
                                passCount++;
                            }
                            break;
                        case "greaternumber":

                            if (reality.ToInt() >= standard.ToInt())
                            {
                                SetPassStyle(index);
                                BaselineList.Rows[index].Cells[6].Value = "符合";
                                passCount++;
                            }
                            break;
                        case "lessnumber":
                            if (reality.ToInt() <= standard.ToInt())
                            {
                                SetPassStyle(index);
                                BaselineList.Rows[index].Cells[6].Value = "符合";
                                passCount++;
                            }
                            break;
                        case "array":
                            if (reality.Equals(standard))
                            {
                                SetPassStyle(index);
                                BaselineList.Rows[index].Cells[6].Value = "符合";
                                passCount++;
                            }
                            break;
                        default:
                            BaselineList.Rows[index].Cells[6].Value = "未定义的类型";
                            break;
                    }
                }
                //统计数据
                totalCount = xmlNodeList.Count;
                totalCountLabel.Text = totalCount.ToString();
                passCountLabel.Text = passCount.ToString();
                failCountLabel.Text = (totalCount - passCount).ToString();
                rate = Convert.ToDouble(passCount) / Convert.ToDouble(totalCount);
                ratePercent = Math.Round(rate, 2) * 100;
                if (ratePercent < 50)
                {
                    rateLabel.ForeColor = Color.Red;
                }
                else if (ratePercent >= 50 && ratePercent < 80)
                {
                    rateLabel.ForeColor = Color.Orange;
                }
                else
                {
                    rateLabel.ForeColor = Color.Green;
                }
                rateLabel.Text = ratePercent + "%";
            }
            catch (Exception ex)
            {
                UIMessageBox.ShowError(ex.Message);
            }
        }

        private void OpenSubKeyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int selectedRowIndex = BaselineList.SelectedCells[0].RowIndex;
            string cellValue = BaselineList[2, selectedRowIndex].Value.ToString();
            if (!cellValue.Contains("HKEY"))
            {
                UIMessageBox.ShowError("该项不支持打开");
                return;
            }
            if (!Util.ExecutExternalProgram(string.Format(openRegCmd, cellValue), true).Equals(0))
            {
                UIMessageBox.ShowError("打开注册表项失败");
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            OSLabel.Text = Util.GetOSVersion();
            IPLabel.Text = Util.GetIPAddress();
            OSNameLabel.Text = Environment.MachineName;
        }

        private void FortifyBtn_Click(object sender, EventArgs e)
        {
            try
            {
                int fortifyCount = 0;
                if (File.Exists(fortifyFile))
                {
                    File.Delete(fortifyFile);
                }
                IniFile iniFile = new IniFile(fortifyFile);
                iniFile.Write("Version", "signature", "$CHICAGO$");
                foreach (DataGridViewRow dataGridViewRow in BaselineList.Rows)
                {
                    if (dataGridViewRow.Cells["FortifyColumn"].Value == null || !(bool)dataGridViewRow.Cells["FortifyColumn"].Value)
                    {
                        continue;
                    }
                    string fortifyItem, fortifyField, fortifyValue = string.Empty;
                    fortifyItem = dataGridViewRow.Cells["ItemColumn"].Value.ToString();
                    fortifyField = dataGridViewRow.Cells["FieldColumn"].Value.ToString();
                    fortifyValue = dataGridViewRow.Cells["StandardColumn"].Value.ToString();
                    if (fortifyItem.Contains("HKEY"))
                    {
                        RegistryHelper.SaveValue(fortifyItem, fortifyField, fortifyValue);
                        fortifyCount++;
                        continue;
                    }
                    //secedit加固
                    iniFile.Write(fortifyItem, fortifyField, fortifyValue);
                    iniFile.UpdateFile();
                    //TODO
                    fortifyCount++;
                }
                int code = Util.ExecutExternalProgram(fortifyCmd, true, ProcessWindowStyle.Hidden);
                if (code != 0)
                {
                    UIMessageBox.ShowWarning(string.Format("已完成{0}项加固操作,secedit加固出现错误.请检查%windir%\\security\\logs\\scesrv.log", fortifyCount.ToString()));
                    return;
                }
                checkAllCheckBox.Checked = false;
                UIMessageBox.ShowSuccess(string.Format("已完成{0}项加固操作,请再次进行检测", fortifyCount.ToString()));
            }
            catch (Exception ex)
            {
                UIMessageBox.ShowError(ex.Message);
            }
        }

        private void checkAllCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (!checkAllCheckBox.Checked)
                {
                    foreach (DataGridViewRow row in BaselineList.Rows)
                    {
                        DataGridViewCell checkBoxCell = row.Cells["FortifyColumn"];
                        /*if (!checkBoxCell.ReadOnly)
                        {
                            checkBoxCell.Value = true;
                        }*/
                        checkBoxCell.Value = false;
                    }
                    return;
                }
                // 遍历 DataGridView 的所有行，并将 "CheckBoxColumn" 列的复选框设置为选中状态
                foreach (DataGridViewRow row in BaselineList.Rows)
                {
                    DataGridViewCell checkBoxCell = row.Cells["FortifyColumn"];
                    if (!checkBoxCell.ReadOnly)
                    {
                        checkBoxCell.Value = true;
                    }
                }
            }
            catch (Exception ex)
            {
                UIMessageBox.ShowError(ex.Message);
            }

        }

        private void ReportBtn_Click(object sender, EventArgs e)
        {
            UIMessageBox.ShowInfo("施工中......");
        }
    }
}
