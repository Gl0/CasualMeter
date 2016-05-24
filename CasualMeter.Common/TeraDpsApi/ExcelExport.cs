using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using CasualMeter.Common.Helpers;
using Tera.Data;
using Tera.Game;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.Drawing;

namespace CasualMeter.Common.TeraDpsApi
{
    class ExcelExport
    {
        private static BasicTeraData BTD = SettingsHelper.Instance.BasicTeraData;
        private static readonly object Savelock = new object();
        public static void ExcelSave(EncounterBase data, TeraData teraData)
        {
            if (!SettingsHelper.Instance.Settings.ExcelExport) return;
            lock (Savelock) //can't save 2 excel files at one time
            {
                NpcInfo Boss = teraData.NpcDatabase.GetOrPlaceholder(ushort.Parse(data.areaId), uint.Parse(data.bossId));
                var dir = Path.Combine(SettingsHelper.Instance.GetDocumentsPath(), $"exports/{Boss.Area.Replace(":", "-")}");
                Directory.CreateDirectory(dir);
                var fname = Path.Combine(dir, $"{Boss.Name.Replace(":", "-")} {DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture)}.xlsx");
                FileInfo file = new FileInfo(fname);
                if (file.Exists) return; //the only case this can happen is BAM mobtraining, that's not so interesting statistic to deal with more complex file names.
                using (ExcelPackage package = new ExcelPackage(file))
                {
                    ExcelWorksheet ws = package.Workbook.Worksheets.Add("Boss");
                    var linkStyle = package.Workbook.Styles.CreateNamedStyle("HyperLink");   //This one is language dependent
                    linkStyle.Style.Font.UnderLine = true;
                    linkStyle.Style.Font.Color.SetColor(Color.Blue);
                    ws.DefaultRowHeight = 30;
                    ws.Cells.Style.Font.Size = 14;
                    ws.Cells.Style.Font.Name = "Arial";
                    ws.Cells[1, 1].Value = $"{Boss.Area}: {Boss.Name} {TimeSpan.FromSeconds(double.Parse(data.fightDuration)).ToString(@"mm\:ss")}";
                    ws.Cells[1, 1, 1, 6].Merge = true;
                    ws.Cells[1, 1, 1, 8].Style.Font.Bold = true;
                    ws.Cells[1, 7].Value = long.Parse(data.partyDps);
                    ws.Cells[1, 7].Style.Numberformat.Format = @"#,#00,\k\/\s";
                    ws.Cells[2, 1].Value = "Ic";
                    ws.Cells[2, 1].Style.Font.Color.SetColor(Color.Transparent);
                    ws.Cells[2, 2].Value = "Name";
                    ws.Cells[2, 3].Value = "Deaths";
                    ws.Cells[2, 4].Value = "Death time";
                    ws.Cells[2, 5].Value = "Damage %";
                    ws.Cells[2, 6].Value = "Crit %";
                    ws.Cells[2, 7].Value = "DPS";
                    ws.Cells[2, 8].Value = "Damage";
                    int i = 2;
                    foreach (var user in data.members.OrderByDescending(x => long.Parse(x.playerTotalDamage)))
                    {
                        i++;
                        ws.Cells[i, 1].Value = i - 2;
                        AddImage(ws, i, 1, Invert(new Bitmap(SettingsHelper.Instance.GetImage((PlayerClass)Enum.Parse(typeof(PlayerClass), user.playerClass)))));
                        ws.Cells[i, 2].Value = $"{user.playerServer}: {user.playerName}";
                        ws.Cells[i, 2].Hyperlink = CreateUserSheet(package.Workbook, user, teraData);
                        ws.Cells[i, 3].Value = long.Parse(user.playerDeaths);
                        ws.Cells[i, 4].Value = long.Parse(user.playerDeathDuration);
                        ws.Cells[i, 4].Style.Numberformat.Format = @"0\s";
                        ws.Cells[i, 5].Value = double.Parse(user.playerTotalDamagePercentage) / 100;
                        ws.Cells[i, 5].Style.Numberformat.Format = "0.0%";
                        ws.Cells[i, 6].Value = double.Parse(user.playerAverageCritRate) / 100;
                        ws.Cells[i, 6].Style.Numberformat.Format = "0.0%";
                        ws.Cells[i, 7].Value = long.Parse(user.playerDps);
                        ws.Cells[i, 7].Style.Numberformat.Format = @"#,#0,\k\/\s";
                        ws.Cells[i, 8].Value = long.Parse(user.playerTotalDamage);
                        ws.Cells[i, 8].Style.Numberformat.Format = @"#,#0,\k";
                    }
                    ws.Cells[1, 8].Formula = $"SUM(H3:H{i})";
                    ws.Cells[1, 8].Style.Numberformat.Format = @"#,#0,\k";
                    var border = ws.Cells[1, 1, i, 8].Style.Border;
                    border.Bottom.Style = border.Top.Style = border.Left.Style = border.Right.Style = ExcelBorderStyle.Thick;
                    ws.Cells[2, 1, i, 8].AutoFilter = true;

                    int j = i + 3;
                    ws.Cells[j, 1].Value = "Ic";
                    ws.Cells[j, 1].Style.Font.Color.SetColor(Color.Transparent);
                    ws.Cells[j, 2].Value = "Debuff name";
                    ws.Cells[j, 2, j, 7].Merge = true;
                    ws.Cells[j, 8].Value = "%";
                    ws.Cells[j, 2, j, 8].Style.Font.Bold = true;
                    foreach (var buf in data.debuffUptime)
                    {
                        j++;
                        var hotdot = teraData.HotDotDatabase.Get(int.Parse(buf.Key));
                        ws.Cells[j, 1].Value = j - i - 3;
                        AddImage(ws, j, 1, BTD.Icons.GetBitmap(hotdot.IconName));
                        ws.Cells[j, 2].Value = hotdot.Name;
                        if (!string.IsNullOrEmpty(hotdot.Tooltip)) ws.Cells[j, 2].AddComment("" + hotdot.Tooltip, "info");
                        ws.Cells[j, 2, j, 7].Merge = true;
                        ws.Cells[j, 8].Value = double.Parse(buf.Value) / 100;
                        ws.Cells[j, 8].Style.Numberformat.Format = "0%";
                    }
                    border = ws.Cells[i + 3, 1, j, 8].Style.Border;
                    border.Bottom.Style = border.Top.Style = border.Left.Style = border.Right.Style = ExcelBorderStyle.Thick;

                    ws.Column(1).Width = 5.6;
                    ws.Column(2).AutoFit();
                    ws.Column(3).AutoFit();
                    ws.Column(4).AutoFit();
                    ws.Column(5).AutoFit();
                    ws.Column(6).AutoFit();
                    ws.Column(7).AutoFit();
                    ws.Column(8).Width = 17;
                    ws.Column(2).Width = GetTrueColumnWidth(ws.Column(2).Width);
                    ws.Column(3).Width = GetTrueColumnWidth(ws.Column(3).Width);
                    ws.Column(4).Width = GetTrueColumnWidth(ws.Column(4).Width);
                    ws.Column(5).Width = GetTrueColumnWidth(ws.Column(5).Width);
                    ws.Column(6).Width = GetTrueColumnWidth(ws.Column(6).Width);
                    ws.Column(7).Width = GetTrueColumnWidth(ws.Column(7).Width);
                    ws.Cells[1, 1, j, 8].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    ws.Cells[1, 1, j, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.PrinterSettings.FitToPage = true;
                    package.Workbook.Properties.Title = Boss.Name;
                    package.Workbook.Properties.Author = "CasualMeter";
                    package.Workbook.Properties.Company = "github.com/lunyx github.com/Gl0 github.com/neowutran";
                    package.Save();
                }
            }
        }
        private static void AddImage(ExcelWorksheet ws, int rowIndex, int columnIndex, Bitmap image)
        {
            ExcelPicture picture = null;
            if (image != null)
            {
                picture = ws.Drawings.AddPicture("pic" + rowIndex + columnIndex, image);
                picture.From.Column = columnIndex-1;
                picture.From.Row = rowIndex-1;
                picture.From.ColumnOff = 12000;
                picture.From.RowOff = 12000;
                picture.SetSize(38, 38);
            }
        }
        private static ExcelHyperLink CreateUserSheet(ExcelWorkbook wb, Members user, TeraData teraData)
        {
            ExcelWorksheet ws = wb.Worksheets.Add($"{user.playerServer}_{user.playerName}");
            var rgc = new RaceGenderClass("Common", "Common", user.playerClass);
            ws.DefaultRowHeight = 30;
            ws.Cells.Style.Font.Size = 14;
            ws.Cells.Style.Font.Name = "Arial";
            AddImage(ws, 1, 1, Invert(new Bitmap(SettingsHelper.Instance.GetImage((PlayerClass)Enum.Parse(typeof(PlayerClass), user.playerClass)))));
            ws.Cells[1, 2].Value = $"{user.playerServer}: {user.playerName}";
            ws.Cells[1, 2, 1, 10].Merge = true;
            ws.Cells[1, 2, 1, 10].Style.Font.Bold = true;
            ws.Cells[2, 2].Value = "Skill";
            ws.Cells[2, 3].Value = "Damage %";
            ws.Cells[2, 4].Value = "Damage";
            ws.Cells[2, 5].Value = "Crit %";
            ws.Cells[2, 6].Value = "Hits";
            ws.Cells[2, 7].Value = "Max Crit";
            ws.Cells[2, 8].Value = "Min Crit";
            ws.Cells[2, 9].Value = "Avg Crit";
            ws.Cells[2, 10].Value = "Avg White";
            int i = 2;
            foreach (var stat in user.skillLog.OrderByDescending(x => long.Parse(x.skillTotalDamage)))
            {
                i++;
                var skill = teraData.SkillDatabase.GetOrNull(rgc, int.Parse(stat.skillId));
                if (skill == null)
                {
                    ws.Cells[i, 1].Value = i-2;
                    ws.Cells[i, 2].Value = "Some pet skill";
                }
                else
                {
                    ws.Cells[i, 1].Value = i - 2;
                    AddImage(ws, i, 1, BTD.Icons.GetBitmap(skill.IconName));
                    ws.Cells[i, 2].Value = skill.Name;
                }
                ws.Cells[i, 3].Value = double.Parse(stat.skillDamagePercent) / 100;
                ws.Cells[i, 3].Style.Numberformat.Format = "0.0%";
                ws.Cells[i, 4].Value = long.Parse(stat.skillTotalDamage);
                ws.Cells[i, 4].Style.Numberformat.Format = @"#,#0,\k";
                ws.Cells[i, 5].Value = double.Parse(stat.skillCritRate) / 100;
                ws.Cells[i, 5].Style.Numberformat.Format = "0.0%";
                ws.Cells[i, 6].Value = long.Parse(stat.skillHits);
                ws.Cells[i, 7].Value = long.Parse(stat.skillHighestCrit);
                ws.Cells[i, 7].Style.Numberformat.Format = @"#,#0,\k";
                ws.Cells[i, 8].Value = long.Parse(stat.skillLowestCrit);
                ws.Cells[i, 8].Style.Numberformat.Format = @"#,#0,\k";
                ws.Cells[i, 9].Value = long.Parse(stat.skillAverageCrit);
                ws.Cells[i, 9].Style.Numberformat.Format = @"#,#0,\k";
                ws.Cells[i, 10].Value = long.Parse(stat.skillAverageWhite);
                ws.Cells[i, 10].Style.Numberformat.Format = @"#,#0,\k";
            }
            var border = ws.Cells[1, 1, i, 10].Style.Border;
            border.Bottom.Style = border.Top.Style = border.Left.Style = border.Right.Style = ExcelBorderStyle.Thick;
            ws.Cells[2, 1, i, 10].AutoFilter = true;

            int j = i + 3;
            ws.Cells[j, 2].Value = "Buff name";
            ws.Cells[j, 2, j, 9].Merge = true;
            ws.Cells[j, 10].Value = "%";
            ws.Cells[j, 2, j, 10].Style.Font.Bold = true;
            foreach (var buf in user.buffUptime)
            {
                j++;
                var hotdot = teraData.HotDotDatabase.Get(int.Parse(buf.Key));
                ws.Cells[j, 1].Value = j - i - 3;
                AddImage(ws, j, 1, BTD.Icons.GetBitmap(hotdot.IconName));
                ws.Cells[j, 2].Value = hotdot.Name;
                if (!string.IsNullOrEmpty(hotdot.Tooltip)) ws.Cells[j, 2].AddComment("" + hotdot.Tooltip, "info");
                ws.Cells[j, 2, j, 9].Merge = true;
                ws.Cells[j, 10].Value = double.Parse(buf.Value) / 100;
                ws.Cells[j, 10].Style.Numberformat.Format = "0%";
                if (!string.IsNullOrEmpty(hotdot.ItemName)) ws.Cells[j, 10].AddComment("" + hotdot.ItemName, "info");
            }
            border = ws.Cells[i + 3, 1, j, 10].Style.Border;
            border.Bottom.Style = border.Top.Style = border.Left.Style = border.Right.Style = ExcelBorderStyle.Thick;

            ws.Column(1).Width = 5.6;
            ws.Column(2).AutoFit();
            ws.Column(3).AutoFit();
            ws.Column(4).AutoFit();
            ws.Column(5).AutoFit();
            ws.Column(6).AutoFit();
            ws.Column(7).AutoFit();
            ws.Column(8).AutoFit();
            ws.Column(9).AutoFit();
            ws.Column(10).AutoFit();
            ws.Column(2).Width = GetTrueColumnWidth(ws.Column(2).Width);
            ws.Column(3).Width = GetTrueColumnWidth(ws.Column(3).Width);
            ws.Column(4).Width = GetTrueColumnWidth(ws.Column(4).Width);
            ws.Column(5).Width = GetTrueColumnWidth(ws.Column(5).Width);
            ws.Column(6).Width = GetTrueColumnWidth(ws.Column(6).Width);
            ws.Column(7).Width = GetTrueColumnWidth(ws.Column(7).Width);
            ws.Column(8).Width = GetTrueColumnWidth(ws.Column(8).Width);
            ws.Column(9).Width = GetTrueColumnWidth(ws.Column(9).Width);
            ws.Column(10).Width = GetTrueColumnWidth(ws.Column(10).Width);
            ws.Cells[1, 1, j, 10].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells[1, 1, j, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.PrinterSettings.FitToPage = true;
            return new ExcelHyperLink($"'{user.playerServer}_{user.playerName}'!A1", $"{user.playerServer}: {user.playerName}");
        }

        private static double GetTrueColumnWidth(double width)
        {
            //DEDUCE WHAT THE COLUMN WIDTH WOULD REALLY GET SET TO
            double z = 1d;
            if (width >= (1 + 2F / 3))
                z = Math.Round((Math.Round(7 * (width - 1F / 256), 0) - 5) / 7, 2);
            else
                z = Math.Round((Math.Round(12 * (width - 1F / 256), 0) - Math.Round(5 * width, 0)) / 12, 2);

            //HOW FAR OFF? (WILL BE LESS THAN 1)
            double errorAmt = width - z;

            //CALCULATE WHAT AMOUNT TO TACK ONTO THE ORIGINAL AMOUNT TO RESULT IN THE CLOSEST POSSIBLE SETTING 
            double adj = 0d;
            if (width >= (1 + 2 / 3))
                adj = (Math.Round(7 * errorAmt - 7F / 256, 0)) / 7;
            else
                adj = ((Math.Round(12 * errorAmt - 12F / 256, 0)) / 12) + (2F / 12);

            //RETURN A SCALED-VALUE THAT SHOULD RESULT IN THE NEAREST POSSIBLE VALUE TO THE TRUE DESIRED SETTING
            if (z > 0)
                return width + adj;

            return 0d;
        }

        private static Bitmap Invert(Bitmap image)
        {
            if (image!=null)
            for (int k = 0; k < image.Width; k++)
            {
                for (int l = 0; l < image.Height; l++)
                {
                    var col = image.GetPixel(k, l);
                    image.SetPixel(k, l, Color.FromArgb(col.A, 255 - (col.R + col.G + col.B) / 3, 255 - (col.R + col.G + col.B) / 3, 255 - (col.R + col.G + col.B) / 3));
                }
            }
            return image;
        }
    }
}
