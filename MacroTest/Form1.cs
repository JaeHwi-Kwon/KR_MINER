using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using OpenCvSharp;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace MacroTest
{

    public partial class Form1 : Form
    {
        [DllImport("user32", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32")]
        private static extern IntPtr FindWindowEx(IntPtr hWnd1, IntPtr hWnd2, string lp1, string lp2);
        [DllImport("user32")]
        internal static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcblt, int nFlags);
        [DllImport("user32")]
        public static extern int SetWindowPos(int hwnd, int hWndInsertAfter, int x, int y, int cx, int cy, int wFlags);
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, IntPtr lParam);
        [DllImport("user32", EntryPoint = "PostMessageA")]
        public static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, IntPtr lParam);
        [DllImport("user32")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);
        [DllImport("user32")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32")]
        private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);
        [DllImport("user32")]
        private static extern IntPtr GetClientRect(IntPtr hWnd, ref Rect rect);
        [DllImport("user32")]
        private static extern IntPtr ScreenToClient(IntPtr hWnd, ref Rect rect);
        [DllImport("user32")]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        [DllImport("user32")]
        public static extern IntPtr WindowFromPoint(System.Drawing.Point pt);

        public enum WMessages : int

        {

            WM_LBUTTONDOWN = 0x0201, //Left mousebutton down

            WM_LBUTTONUP = 0x0202,  //Left mousebutton up

            WM_LBUTTONDBLCLK = 0x203, //Left mousebutton doubleclick

            WM_RBUTTONDOWN = 0x204, //Right mousebutton down

            WM_RBUTTONUP = 0x205,   //Right mousebutton up

            WM_RBUTTONDBLCLK = 0x206, //Right mousebutton doubleclick

            WM_KEYDOWN = 0x100,  //Key down

            WM_KEYUP = 0x101,   //Key up

            WM_SYSKEYDOWN = 0x104,

            WM_SYSKEYUP = 0x105,

            WM_CHAR = 0x102,

            WM_COMMAND = 0x111

        }
        
        public enum AppPlayer
        {
            BlueStacks, LDPlayer, NOX
        }

        public enum ImgSearchIdx
        {
            INVALID_IDX, ITEM_SUMMON_BUTTON, INVENTORY_BACK, GOLD_STORE_BUTTON, SUMMON_BUTTON, NORMAL_SUMMON_BUTTON, 
            NORMAL_GOLD_SUMMON_BUTTON, SELL_BUTTON, BULK_SELL_BUTTON, BULK_SELL_ALL, BULK_SELL_CONFIRM,
            BACKPACK_ICON, EVENT_POP_UP_EXIT, COMMUNITY_POP_UP_EXIT, CALENDER_EXIT, APP_PLAYER_MAIN,
            SUMMON_BACKPACK_FULL, SUMMON_GOLD_OUT
        }

        public class ImgSearchInfo
        {
            public string ImgLoc;
            public System.Drawing.Point SearchPt;
            public System.Drawing.Point ClickPt;
            public double click_latency = 1;
            public System.Drawing.Point SearchSize;

            public ImgSearchInfo(string Loc, System.Drawing.Point sPt, System.Drawing.Point cPt,
                System.Drawing.Point size, double latency = 1)
            {
                ImgLoc = Loc;
                SearchPt = sPt;
                ClickPt = cPt;
                click_latency = latency;
            }

            public void ChangeImg(int x, int y, double latency)
            {
                SearchPt.X = x - SearchSize.X/2; SearchPt.Y = y - SearchSize.Y/2;
                ClickPt.X = x;  ClickPt.Y = y;
                click_latency = latency;
            }
            public void SetClickPt(int x, int y)
            {
                ClickPt.X = x;
                ClickPt.Y = y;
            }
            
        }

        class AppInfo_LDP {
            public static KeyValuePair<string, string> WndParent = new KeyValuePair<string, string>( "LDPlayerMainFrame", null );
            public static KeyValuePair<string, string> WndChild = new KeyValuePair<string, string>("RenderWindow", null);
            public static KeyValuePair<string, string> TargetWnd = new KeyValuePair<string, string>("subWin", null);
        }
        class AppInfo_BLU {
            public static KeyValuePair<string, string> WndParent = new KeyValuePair<string, string>(null, "BlueStacks");
            public static KeyValuePair<string, string> WndChild = new KeyValuePair<string, string>(null, "BlueStacks Android PluginAndroid");
            public static KeyValuePair<string, string> TargetWnd = new KeyValuePair<string, string>(null, "_ctl.Window");
        }
        //class AppInfo_NOX {
        //    public const int wndSizeX = 2;
        //    public const int wndSizeY = 48;
        //    public static KeyValuePair<string, string> WndParent = new KeyValuePair<string, string>("Qt5QWindowIcon", null);
        //    public static KeyValuePair<string, string> WndChild = new KeyValuePair<string, string>(null, "ScreenBoardClassWindow");
        //    public static KeyValuePair<string, string> TargetWnd = new KeyValuePair<string, string>(null, "sub");
        //}
        

        public IntPtr ParentwHnd = IntPtr.Zero;
        public IntPtr ChildwHnd = IntPtr.Zero;
        public IntPtr TrgwHnd = IntPtr.Zero;
        public Bitmap screenshot = null;
        public Bitmap screenshotcomp = null;
        bool isBusy = false;
        public int gachaCnt;
        public int frzCnt;
        public int iconx = 298;
        public int icony = 204;
        public int exitx;
        public int exity;
        public int wndsizeX;
        public int wndsizeY;
        public Thread[] threads = new Thread[2];
        public bool[] threadEnabled = new bool[2];
        public int threadcnt = 0;

        public static string JsonFile = @".\preset.json";
        public static string SearchInfoJson = @".\imgInfo.json";
        public double imgacr = 80.0;
        public double latencytweaker = 1.0;
        public int generalgoldcycle = 6;
        public int imgResetFlag = 0;
        public int gamestoppedlatency = 20;
        public Process[] processes;
        public string date = "13";
        public List<IntPtr> AppPlayerHandles = new List<IntPtr>();

        public List<ImgSearchInfo> ImgInfoList = new List<ImgSearchInfo>();
        
        

        public Form1()
        {
            InitializeComponent();
            for(int i = 0; i < 2; ++i)
            {
                threadEnabled[i] = false;
                threads[i] = null;
            }
            int app = WhichAppPlayer();
            //FindAllTheAppPlayerWnd();
            
            FindWnd(app);
            gachaCnt = 0;

            LoadPreset();

            // 콤보 박스 리스트 생성
            string[] data = {   "골드 뽑기 버튼", "인벤토리 뒤로가기", "특별상점-골드 상점 버튼",
                                "소환 버튼","일반소환 버튼","일반소환-골드소환 버튼", "판매 버튼", "일괄판매 버튼", 
                                "일괄판매-판매하기 버튼", "일괄판매-판매하기 확인버튼", "가방 아이콘", "이벤트 팝업창",
                                "커뮤니티 팝업창", "출석체크","킹스레이드 아이콘", "소환-가방꽉참", "소환-골드 부족" };
            // 생성된 리스트 콤보 박스에 저장
            comboBox1.Items.AddRange(data);

        }

        public void FindAllTheAppPlayerWnd()
        {
            processes = Process.GetProcesses();
            foreach (var p in processes)
            {
                //textBox1.AppendText(p.ProcessName.ToString() + "\r\n");
                //// 클래스 이름 검색
                //StringBuilder className = new StringBuilder(255);
                //GetClassName(p.MainWindowHandle, className, 255);
                //string classNameAsStr = className.ToString();
                //textBox1.AppendText(classNameAsStr + "\r\n");
                //bool isContained = classNameAsStr.Contains("BlueStacks");
                //if (isContained)
                //{
                //    textBox1.AppendText(p.Id + "에 블루스택스 문자가 있습니다. \r\n");
                //    AppPlayerHandles.Add(p.MainWindowHandle);
                //    FindCtrlHandles(classNameAsStr, (IntPtr)p.MainWindowHandle);
                //}
                if (p.ProcessName.Contains("Bluestacks"))
                {
                    textBox1.AppendText(p.Id + "\r\n");
                }
            }
        }

        public int FindCtrlHandles(string ClassName, IntPtr hWnd)
        {
            List<IntPtr> result = new List<IntPtr>();
            bool isContained = false;
            if (ClassName.Contains("BlueStacks")) {
                IntPtr CurWnd = IntPtr.Zero;
                IntPtr PrevWnd = IntPtr.Zero;
                while (true)
                {
                    CurWnd = FindWindowEx(hWnd, PrevWnd, null, null);
                    if (CurWnd == IntPtr.Zero) break;
                    result.Add(CurWnd);
                    PrevWnd = CurWnd;
                }
                foreach (var l in result)
                    textBox1.AppendText(l.ToString());
                return 1;
            }
            if (ClassName.Contains(AppInfo_LDP.WndParent.Value)) {
                return 1;
            }
            return 0;
        }

        public void LoadPreset()
        {
            if (!File.Exists(JsonFile))
            {
                WritePreset(0);
            }
            using (StreamReader file =  File.OpenText(JsonFile))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                JObject json = (JObject)JToken.ReadFrom(reader);
                gamestoppedlatency = (int)json["gamestoppedlatency"];
                generalgoldcycle = (int)json["generalgoldcycle"];
                latencytweaker = (double)json["latencytweaker"];
                iconx = (int)json["iconx"];
                icony = (int)json["icony"];
                exitx = (int)json["exitx"];
                exity = (int)json["exity"];
                imgacr = (double)json["imgacr"];
            }

            textBox4.Text = iconx.ToString();
            textBox5.Text = icony.ToString();
            textBox7.Text = exitx.ToString();
            textBox8.Text = exity.ToString();

            textBox12.Text = generalgoldcycle.ToString();
            textBox13.Text = latencytweaker.ToString();
            textBox14.Text = gamestoppedlatency.ToString();

            if (!File.Exists(SearchInfoJson))
            {
                WriteSearchInfo(0);
            }
            using(StreamReader file = File.OpenText(SearchInfoJson))
            {
                string infofileasStr = file.ReadToEnd();
                //textBox1.AppendText(infofileasStr);
                ImgInfoList.Clear();
                ImgInfoList = JsonConvert.DeserializeObject<List<ImgSearchInfo>>(infofileasStr);
            }
        }

        private void WriteSearchInfo(int flag)
        {
            //                public string ImgLoc;
            //public System.Drawing.Point SearchPt;
            //public System.Drawing.Point ClickPt;
            //public double click_latency = 1;
            //public System.Drawing.Point SearchSize;

            if (flag == 0)
            {
                string jsonStr = JsonConvert.SerializeObject(ImgInfoList.ToArray(), Formatting.Indented);
                System.IO.File.WriteAllText(SearchInfoJson,jsonStr);
            }
            else
            {
                string jsonStr = JsonConvert.SerializeObject(ImgInfoList.ToArray(), Formatting.Indented);
                System.IO.File.WriteAllText(SearchInfoJson, jsonStr);
            }
        }

        // 0이면 초기값 입력, 1이면 현재 설정값 입력
        private void WritePreset(int flag)
        {
            if (flag == 0)
            {
                JObject preset = new JObject(
                    new JProperty("gamestoppedlatency", 20),
                    new JProperty("generalgoldcycle", 6),
                    new JProperty("latencytweaker", 1.0),
                    new JProperty("iconx", 0),
                    new JProperty("icony", 0),
                    new JProperty("exitx", 0),
                    new JProperty("exity", 0),
                    new JProperty("imgacr", 80.0));
                File.WriteAllText(JsonFile, preset.ToString());
            }
            else
            {
                JObject preset = new JObject(
                    new JProperty("gamestoppedlatency", gamestoppedlatency),
                    new JProperty("generalgoldcycle", generalgoldcycle),
                    new JProperty("latencytweaker", latencytweaker),
                    new JProperty("iconx", iconx),
                    new JProperty("icony", icony),
                    new JProperty("exitx", exitx),
                    new JProperty("exity", exity),
                    new JProperty("imgacr", imgacr));
                File.WriteAllText(JsonFile, preset.ToString());
            }
        }

        public int WhichAppPlayer()
        {
            if (FindWindow(AppInfo_LDP.WndParent.Key, AppInfo_LDP.WndParent.Value) != IntPtr.Zero)
            {
                return (int)AppPlayer.LDPlayer;
            }
            else if (FindWindow(AppInfo_BLU.WndParent.Key, AppInfo_BLU.WndParent.Value) != IntPtr.Zero)
            {
                return (int)AppPlayer.BlueStacks;
            }
            //else if (FindWindow(AppInfo_NOX.WndParent.Key, AppInfo_NOX.WndParent.Value) != IntPtr.Zero)
            //{
            //    return (int)AppPlayer.NOX;
            //}

            return -1;
        }

        // img를 비교대상 comparison과 비교한다.
        // 비교 범위는 x,y 좌표상의 height,width 크기만큼 잘라내어 비교한다.
        // 결과는 그 유사도를 출력한다.
        public double ImgCompare(ref Bitmap img, ref Bitmap comparison, int x, int y, int height, int width)
        {
            if (x < 0) x = 0;
            if (y < 0) y = 0;
            double minval, maxval = 0;
            OpenCvSharp.Point minloc, maxloc;
            try
            {
                using (Mat imgMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(img))
                using (Mat compMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(comparison))
                {
                    var imgMat_conv = imgMat.Clone(new Rect(x, y, height, width));
                    var compMat_conv = compMat.Clone(new Rect(x, y, height, width));
                    using (Mat res = imgMat_conv.MatchTemplate(compMat_conv, TemplateMatchModes.CCoeffNormed))
                    {
                        Cv2.MinMaxLoc(res, out minval, out maxval, out minloc, out maxloc);
                        var newpic_img = imgMat_conv;
                        var newpic_comp = compMat_conv;
                        pictureBox1.Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(newpic_img);
                        pictureBox2.Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(newpic_comp);
                        textBox15.Text = "일치율 : " + (Math.Truncate(maxval * 10000) / 100).ToString() + " %";
                        if (textBox15.TextLength > 1000) textBox15.Clear();
                        return maxval;
                    }
                }
            }
            catch
            {

                return 0;
            }
        }

        public void MouseClick(int x, int y)
        {
            if (ParentwHnd != IntPtr.Zero)
            {
                IntPtr lparam = new IntPtr(x | (y << 16));

                //textBox1.Text += "클릭할 대상 화면" + TrgwHnd + "\r\n";

                SendMessage(ChildwHnd, (int)WMessages.WM_LBUTTONDOWN, 1, lparam);
                SendMessage(ChildwHnd, (int)WMessages.WM_LBUTTONUP, 0, lparam);
            }
        }

        public void FindWnd(int app)
        {
            switch (app)
            {
                case (int)AppPlayer.BlueStacks:
                    textBox9.Text = "You're Running BlueStacks";
                    ParentwHnd = FindWindow(AppInfo_BLU.WndParent.Key, AppInfo_BLU.WndParent.Value);
                    ChildwHnd = FindWindowEx(ParentwHnd, IntPtr.Zero, AppInfo_BLU.WndChild.Key, AppInfo_BLU.WndChild.Value);
                    TrgwHnd = FindWindowEx(ChildwHnd, IntPtr.Zero, AppInfo_BLU.TargetWnd.Key, AppInfo_BLU.TargetWnd.Value);
                    break;
                //case (int)AppPlayer.NOX:
                //    wndsizeX = AppInfo_NOX.wndSizeX;
                //    wndsizeY = AppInfo_NOX.wndSizeY;
                //    textBox9.Text = "You're Running NOX Player";
                //    ParentwHnd = FindWindow(AppInfo_NOX.WndParent.Key, AppInfo_NOX.WndParent.Value);
                //    ChildwHnd = FindWindowEx(ParentwHnd, IntPtr.Zero, AppInfo_NOX.WndChild.Key, AppInfo_NOX.WndChild.Value);
                //    TrgwHnd = FindWindowEx(ChildwHnd, IntPtr.Zero, AppInfo_NOX.TargetWnd.Key, AppInfo_NOX.TargetWnd.Value);
                //    SetWindowPos((int)ParentwHnd, 0, 0, 0, 1280 + wndsizeX, 720 + wndsizeY, 0x10);
                    return;
                case (int)AppPlayer.LDPlayer:
                    textBox9.Text = "You're Running LDPlayer";
                    ParentwHnd = FindWindow(AppInfo_LDP.WndParent.Key, AppInfo_LDP.WndParent.Value);
                    ChildwHnd = FindWindowEx(ParentwHnd, IntPtr.Zero, AppInfo_LDP.WndChild.Key, AppInfo_LDP.WndChild.Value);
                    TrgwHnd = FindWindowEx(ChildwHnd, IntPtr.Zero, AppInfo_LDP.TargetWnd.Key, AppInfo_LDP.TargetWnd.Value);
                    break;
            }
            Rect WndRect = new Rect();
            Rect ClntRect = new Rect();
            int oldsizeX = 0, oldsizeY = 0;
            GetClientRect(ParentwHnd, ref WndRect);
            GetClientRect(TrgwHnd, ref ClntRect);

            wndsizeX = WndRect.Right - ClntRect.Right;
            wndsizeY = WndRect.Bottom - ClntRect.Bottom;

            textBox1.AppendText("현재 윈도우 영역의 크기 : " + WndRect.Bottom + ", " + WndRect.Right + "\r\n");

            textBox1.AppendText("윈도우 : " + WndRect.Top + ", " + WndRect.Left + ", " + WndRect.Bottom + ". " + WndRect.Right + "\r\n");
            textBox1.AppendText("클라이언트 : " + ClntRect.Top + ", " + ClntRect.Left + ", " + ClntRect.Bottom + ". " + ClntRect.Right + "\r\n");


            while ((oldsizeX - wndsizeX) != 0 || (oldsizeY - wndsizeY) != 0)
            {
                oldsizeX = wndsizeX;
                oldsizeY = wndsizeY;

                SetWindowPos((int)ParentwHnd, 0, 0, 0, 1280 + wndsizeX, 720 + wndsizeY, 0x10);

                GetClientRect(ParentwHnd, ref WndRect);
                GetClientRect(TrgwHnd, ref ClntRect);

                wndsizeX = WndRect.Right - ClntRect.Right;
                wndsizeY = WndRect.Bottom - ClntRect.Bottom;

                textBox1.AppendText("클라이언트 X축 차이 : " + wndsizeX + "\r\n클라이언트 Y축 차이 : " + wndsizeY + "\r\n");
                
            }
            textBox1.AppendText("변경 후 클라이언트 영역의 크기 : " + ClntRect.Bottom + ", " + ClntRect.Right + "\r\n");
            textBox1.AppendText("X: " + wndsizeX + " Y : " + wndsizeY + "\r\n");
        }

        int cnt = 0;

        //1번 버튼 해당 함수
        private void button1_Click(object sender, EventArgs e) // 시작 버튼
        {
            isBusy = false;

            // 텍스트 변수화
            iconx = Convert.ToInt32(textBox4.Text);
            icony = Convert.ToInt32(textBox5.Text);

            exitx = Convert.ToInt32(textBox7.Text);
            exity = Convert.ToInt32(textBox8.Text);

            latencytweaker = Convert.ToDouble(textBox13.Text);
            generalgoldcycle = Convert.ToInt32(textBox12.Text);
            gamestoppedlatency = Convert.ToInt32(textBox14.Text);

            imgacr = Convert.ToDouble(textBox10.Text);

            WritePreset(1);

            textBox4.ReadOnly = true;
            textBox5.ReadOnly = true;
            textBox7.ReadOnly = true;
            textBox8.ReadOnly = true;
            textBox10.ReadOnly = true;
            textBox12.ReadOnly = true;
            textBox13.ReadOnly = true;
            textBox14.ReadOnly = true; 
            comboBox1.Enabled = false;
            textBox16.ReadOnly = true;

            textBox1.AppendText("튕겼을 시 누를 아이콘의 좌표 : " + iconx + ", " + icony + "\r\n");
            textBox1.AppendText("튕겼을 시 누를 종료버튼 좌표 : " + exitx + ", " + exity + "\r\n");

            cnt = 60;
            timer1.Enabled = true;
            Graphics gdata = Graphics.FromHwnd(TrgwHnd);
            Rectangle rect = Rectangle.Round(gdata.VisibleClipBounds);
            Bitmap bmp = new Bitmap(rect.Width, rect.Height);


            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(0, 0, 0, 0, rect.Size);
                IntPtr hdc = g.GetHdc();
                try
                {
                    PrintWindow(TrgwHnd, hdc, 0x2);
                }
                catch{ }
                g.ReleaseHdc(hdc);
            }
            screenshot = bmp;
            screenshotcomp = bmp;
            screenshot.Save(@".\tempscr.bmp");
            timer2.Enabled = true;
            timer3.Enabled = false;

            
        }
        // 2번 버튼 해당 함수
        private void button2_Click(object sender, EventArgs e) // 종료
        {

            timer1.Enabled = false;
            timer2.Enabled = false;
            timer3.Enabled = true;

            threadEnabled[0] = false;
            threadEnabled[1] = false;

            textBox4.ReadOnly = false;
            textBox5.ReadOnly = false;
            textBox7.ReadOnly = false;
            textBox8.ReadOnly = false;
            textBox10.ReadOnly = false;
            textBox12.ReadOnly = false;
            textBox13.ReadOnly = false;
            textBox14.ReadOnly = false;
            comboBox1.Enabled = true;
            textBox16.ReadOnly = false;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //textBox1.AppendText("타이머1 호출 \r\n");
            cnt += 1;
            Graphics gdata = Graphics.FromHwnd(TrgwHnd);
            Rectangle rect = Rectangle.Round(gdata.VisibleClipBounds);
            Bitmap bmp = new Bitmap(rect.Width, rect.Height);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(0, 0, 0, 0, rect.Size);
                IntPtr hdc = g.GetHdc();
                PrintWindow(TrgwHnd, hdc, 0x2);
                g.ReleaseHdc(hdc);
            }
            //pictureBox1.Image = bmp;
            screenshot = bmp;
            bmp = null;

            if (!isBusy)
            {
                //textBox1.AppendText("골뽑버튼스캔중 \r\n");
                Start_Gacha_Seq();
                timer2.Start();
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            textBox3.Text = frzCnt + " 초 멈춰있습니다.";
            if (screenshot != null)
            {
                
                if (ImgCompare(ref screenshot, ref screenshotcomp, 0, 0, 800, 600) >= 0.99)
                {
                    frzCnt += 1;
                    //textBox1.Text += "멈춤인식 : " + frzCnt / 10 + " 초\r\n";
                    if (frzCnt > gamestoppedlatency)
                    {
                        textBox1.Text += "화면이 멈춘 것 같아 재가동합니다.\r\n";
                        threadEnabled[0] = false;
                        if (threads[0] != null)
                        {
                            while (threads[0].IsAlive == true) { }
                        }
                        frzCnt = 0;
                        isBusy = true;
                        // start reboot sequence
                        Thread t2 = new Thread(Start_Reboot_Seq);
                        threads[1] = t2;
                        threadEnabled[1] = true;
                        timer2.Stop();
                        t2.Start();
                    }
                }
                else
                {
                    frzCnt = 0;
                }
                screenshotcomp = screenshot;
            }
        }

        // 1번 쓰레드
        private void Start_Reboot_Seq()
        {
            Bitmap srcimg;
            ImgSearchInfo info;
            double latency;
            // 블루스택스 윈도우를 최상위에 둔다
            SetForegroundWindow(ParentwHnd);
            // 물리적 마우스를 종료 버튼에 위치하게 하고 클릭한다.
            Cursor.Position = new System.Drawing.Point(exitx, exity);
            mouse_event(0x002, 0, 0, 0, 0);
            Thread.Sleep(10);
            mouse_event(0x004, 0, 0, 0, 0);

            latency = 3000 * latencytweaker;
            Thread.Sleep((int)latency);
            Cursor.Position = new System.Drawing.Point(iconx, icony);
            mouse_event(0x002, 0, 0, 0, 0);
            Thread.Sleep(10);
            mouse_event(0x004, 0, 0, 0, 0);

            Thread.Sleep((int)latency);

            if (date != DateTime.Now.ToString("dd"))
            {

            }

            // 가방 아이콘 보일 때까지 중앙 살짝 아래 반복클릭
            info = ImgInfoList[(int)ImgSearchIdx.BACKPACK_ICON - 1];
            srcimg = (Bitmap)System.Drawing.Bitmap.FromFile(info.ImgLoc);
            while (ImgCompare(ref screenshot, ref srcimg,
                info.SearchPt.X, info.SearchPt.Y, info.SearchSize.X, info.SearchSize.Y) < imgacr / 100)
            {
                MouseClick(650 + 15, 577 + 15);
                if (threadEnabled[1] == false)
                {
                    isBusy = false; return;
                }
                Thread.Sleep(1000);
            }
            // 가방 발견, 가방 클릭
            srcimg.Dispose();
            srcimg = null;
            latency = info.click_latency * latencytweaker*1000;
            Thread.Sleep((int)latency);
            MouseClick(info.ClickPt.X, info.ClickPt.Y);

            // 골드 상점 버튼 보일 때까지 대기
            info = ImgInfoList[(int)ImgSearchIdx.GOLD_STORE_BUTTON - 1];
            srcimg = (Bitmap)System.Drawing.Bitmap.FromFile(info.ImgLoc);
            while (ImgCompare(ref screenshot, ref srcimg,
                info.SearchPt.X, info.SearchPt.Y, info.SearchSize.X, info.SearchSize.Y) < imgacr / 100)
            {
                Thread.Sleep(100);
                if (threadEnabled[1] == false)
                {
                    isBusy = false; return;
                }
            }
            // 골드 상점 아이콘 발견, 클릭
            srcimg.Dispose();
            srcimg = null;
            latency = info.click_latency * latencytweaker * 1000;
            Thread.Sleep((int)latency);
            MouseClick(info.ClickPt.X, info.ClickPt.Y);


            // 소환 탭 보일 때까지 대기
            info = ImgInfoList[(int)ImgSearchIdx.SUMMON_BUTTON - 1];
            srcimg = (Bitmap)System.Drawing.Bitmap.FromFile(info.ImgLoc);
            while (ImgCompare(ref screenshot, ref srcimg,
                info.SearchPt.X, info.SearchPt.Y, info.SearchSize.X, info.SearchSize.Y) < imgacr / 100)
            {
                Thread.Sleep(100);
                if (threadEnabled[1] == false)
                {
                    isBusy = false; return;
                }
            }
            // 소환 탭 발견, 클릭
            srcimg.Dispose();
            srcimg = null;
            latency = info.click_latency * latencytweaker * 1000;
            Thread.Sleep((int)latency);
            MouseClick(info.ClickPt.X, info.ClickPt.Y);


            // 일반 소환 보일 때까지 대기
            info = ImgInfoList[(int)ImgSearchIdx.NORMAL_SUMMON_BUTTON - 1];
            srcimg = (Bitmap)System.Drawing.Bitmap.FromFile(info.ImgLoc);
            while (ImgCompare(ref screenshot, ref srcimg,
                info.SearchPt.X, info.SearchPt.Y, info.SearchSize.X, info.SearchSize.Y) < imgacr / 100)
            {
                Thread.Sleep(100);
                if (threadEnabled[1] == false)
                {
                    isBusy = false; return;
                }
            }
            // 일반 소환 탭 발견, 클릭
            srcimg.Dispose();
            srcimg = null;
            latency = info.click_latency * latencytweaker * 1000;
            Thread.Sleep((int)latency);
            MouseClick(info.ClickPt.X, info.ClickPt.Y);


            // 일반 골드 소환 탭 보일 때까지 대기
            info = ImgInfoList[(int)ImgSearchIdx.NORMAL_GOLD_SUMMON_BUTTON - 1];
            srcimg = (Bitmap)System.Drawing.Bitmap.FromFile(info.ImgLoc);
            while (ImgCompare(ref screenshot, ref srcimg,
                info.SearchPt.X, info.SearchPt.Y, info.SearchSize.X, info.SearchSize.Y) < imgacr / 100)
            {
                Thread.Sleep(100);
                if (threadEnabled[1] == false)
                {
                    isBusy = false; return;
                }
            }
            //일반 골드 소환 탭 발견, 클릭
            srcimg.Dispose();
            srcimg = null;
            latency = info.click_latency * latencytweaker * 1000;
            Thread.Sleep((int)latency);
            MouseClick(info.ClickPt.X, info.ClickPt.Y);

            isBusy = false;
        }

        public void Start_Gacha_Seq()
        {
            ImgSearchInfo info;
            Bitmap srcimg;
            double latency;

            info = ImgInfoList[(int)ImgSearchIdx.SUMMON_GOLD_OUT - 1];
            srcimg = (Bitmap)System.Drawing.Bitmap.FromFile(info.ImgLoc);
            if (ImgCompare(ref screenshot, ref srcimg,
                info.SearchPt.X, info.SearchPt.Y, info.SearchSize.X, info.SearchSize.Y) >= imgacr / 100)
            {
                timer1.Enabled = false;
                timer2.Enabled = false;
                timer3.Enabled = true;

                threadEnabled[0] = false;
                threadEnabled[1] = false;

                textBox4.ReadOnly = false;
                textBox5.ReadOnly = false;
                textBox7.ReadOnly = false;
                textBox8.ReadOnly = false;
                textBox10.ReadOnly = false;
                textBox12.ReadOnly = false;
                textBox13.ReadOnly = false;
                textBox14.ReadOnly = false;
                comboBox1.Enabled = true;
                textBox16.Enabled = true;

                textBox1.AppendText("골드가 다 소진되었습니다.\r\n");


                srcimg.Dispose();
                srcimg = null;
                return;
            }
            srcimg.Dispose();
            srcimg = null;
            info = ImgInfoList[(int)ImgSearchIdx.SUMMON_BACKPACK_FULL - 1];
            srcimg = (Bitmap)System.Drawing.Bitmap.FromFile(info.ImgLoc);
            if (ImgCompare(ref screenshot, ref srcimg,
                info.SearchPt.X, info.SearchPt.Y, info.SearchSize.X, info.SearchSize.Y) >= imgacr / 100)
            {
                if (gachaCnt > 0) gachaCnt -= 1;
                textBox1.AppendText("가방꽉참확인 인식. \r\n");
                latency = info.click_latency * latencytweaker * 1000;
                Thread.Sleep((int)latency);
                MouseClick(info.ClickPt.X, info.ClickPt.Y);
                srcimg.Dispose();
                srcimg = null;
                Thread t1 = new Thread(Start_BackPack_Seq);
                threads[0] = t1;
                threadEnabled[0] = true;
                isBusy = true;
                t1.Start();
                return;
            }

            srcimg.Dispose();
            srcimg = null;
            info = ImgInfoList[(int)ImgSearchIdx.ITEM_SUMMON_BUTTON - 1];
            srcimg = (Bitmap)System.Drawing.Bitmap.FromFile(info.ImgLoc);
            if (ImgCompare(ref screenshot, ref srcimg,
                info.SearchPt.X, info.SearchPt.Y, info.SearchSize.X, info.SearchSize.Y) >= imgacr / 100)
            {
                if (cnt >= generalgoldcycle * 10)
                {
                    latency = info.click_latency * latencytweaker * 1000;
                    Thread.Sleep((int)latency);
                    //textBox1.Text += "소환버튼 인식 \r\n";
                    MouseClick(info.ClickPt.X, info.ClickPt.Y);
                    gachaCnt += 1;
                    textBox2.Text = "누적횟수 : " + gachaCnt;
                    cnt = 0;
                    srcimg.Dispose();
                    srcimg = null;
                }
            }
            else
            {
                srcimg.Dispose();
                srcimg = null;
            }
        }

        // 0번 쓰레드
        public void Start_BackPack_Seq()
        {
            Bitmap srcimg;
            ImgSearchInfo info;
            double latency;

            // 판매 버튼이 보일 때까지 대기
            info = ImgInfoList[(int)ImgSearchIdx.SELL_BUTTON - 1];
            srcimg = (Bitmap)System.Drawing.Bitmap.FromFile(info.ImgLoc);
            while (ImgCompare(ref screenshot, ref srcimg,
                info.SearchPt.X, info.SearchPt.Y, info.SearchSize.X, info.SearchSize.Y) < imgacr / 100) 
            { 
                Thread.Sleep(100);
                if (threadEnabled[0] == false)
                {
                    isBusy = false; return;
                }
            }

            // 판매 버튼 발견, 클릭
            srcimg.Dispose();
            latency = info.click_latency * latencytweaker * 1000;
            Thread.Sleep((int)latency);
            MouseClick(info.ClickPt.X, info.ClickPt.Y);


            // 일괄 판매 버튼이 보일 때까지 대기
            info = ImgInfoList[(int)ImgSearchIdx.BULK_SELL_BUTTON - 1];
            srcimg = (Bitmap)System.Drawing.Bitmap.FromFile(info.ImgLoc);
            while (ImgCompare(ref screenshot, ref srcimg,
                info.SearchPt.X, info.SearchPt.Y, info.SearchSize.X, info.SearchSize.Y) < imgacr / 100)
            {
                Thread.Sleep(100);
                if (threadEnabled[0] == false)
                {
                    isBusy = false; return;
                }
            }
            // 일괄 판매 버튼 발견, 클릭
            srcimg.Dispose();
            latency = info.click_latency * latencytweaker * 1000;
            Thread.Sleep((int)latency);
            MouseClick(info.ClickPt.X, info.ClickPt.Y);


            // 일괄판매 - 판매 버튼이 보일 때까지 대기
            info = ImgInfoList[(int)ImgSearchIdx.BULK_SELL_ALL - 1];
            srcimg = (Bitmap)System.Drawing.Bitmap.FromFile(info.ImgLoc);
            while (ImgCompare(ref screenshot, ref srcimg,
                info.SearchPt.X, info.SearchPt.Y, info.SearchSize.X, info.SearchSize.Y) < imgacr / 100)
            {
                Thread.Sleep(100);
                if (threadEnabled[0] == false)
                {
                    isBusy = false; return;
                }
            }
            // 일괄판매 - 판매 버튼 발견, 클릭
            srcimg.Dispose();
            latency = info.click_latency * latencytweaker * 1000;
            Thread.Sleep((int)latency);
            MouseClick(info.ClickPt.X, info.ClickPt.Y);


            // 일괄 판매 판매 확인 버튼 보일 때까지 대기
            info = ImgInfoList[(int)ImgSearchIdx.BULK_SELL_CONFIRM - 1];
            srcimg = (Bitmap)System.Drawing.Bitmap.FromFile(info.ImgLoc);
            while (ImgCompare(ref screenshot, ref srcimg,
                info.SearchPt.X, info.SearchPt.Y, info.SearchSize.X, info.SearchSize.Y) < imgacr / 100)
            {
                Thread.Sleep(100);
                if (threadEnabled[0] == false)
                {
                    isBusy = false; return;
                }
            }
            // 일괄 판매 판매확인 버튼 발견, 클릭
            srcimg.Dispose();
            latency = info.click_latency * latencytweaker * 1000;
            Thread.Sleep((int)latency);
            MouseClick(info.ClickPt.X, info.ClickPt.Y);


            // 인벤토리 - 뒤로가기 버튼 보일 때까지 대기
            info = ImgInfoList[(int)ImgSearchIdx.INVENTORY_BACK - 1];
            srcimg = (Bitmap)System.Drawing.Bitmap.FromFile(info.ImgLoc);
            while (ImgCompare(ref screenshot, ref srcimg,
                info.SearchPt.X, info.SearchPt.Y, info.SearchSize.X, info.SearchSize.Y) < imgacr / 100)
            {
                Thread.Sleep(100);
                if (threadEnabled[0] == false)
                {
                    isBusy = false; return;
                }
            }
            // 뒤로가기 버튼 발견, 다음 시퀀스 시작
            srcimg.Dispose();
            latency = info.click_latency * latencytweaker * 1000;
            Thread.Sleep((int)latency);

            // 일반소환 - 골드 뽑기 버튼 발견할 때까지 인벤토리 - 뒤로가기 버튼 반복클릭
            info = ImgInfoList[(int)ImgSearchIdx.NORMAL_GOLD_SUMMON_BUTTON - 1];
            srcimg = (Bitmap)System.Drawing.Bitmap.FromFile(info.ImgLoc);
            while (ImgCompare(ref screenshot, ref srcimg,
                info.SearchPt.X, info.SearchPt.Y, info.SearchSize.X, info.SearchSize.Y) < imgacr / 100)
            {
                Thread.Sleep(1000);
                MouseClick(100 + 30, 0 + 20);
                Thread.Sleep(100);
                if (threadEnabled[0] == false)
                {
                    isBusy = false; 
                    return;
                }
            }

            // 일반소환- 골드 뽑기 버튼 발견, 클릭 후 쓰레드 종료
            srcimg.Dispose();
            latency = info.click_latency * latencytweaker * 1000;
            Thread.Sleep((int)latency);
            MouseClick(info.ClickPt.X, info.ClickPt.Y);
            Thread.Sleep(500);

            isBusy = false;
        }

        // 현재 화면 스크린샷 후 sample.bmp 파일로 저장
        private void button3_Click(object sender, EventArgs e)
        {
            Graphics gdata = Graphics.FromHwnd(TrgwHnd);
            Rectangle rect = Rectangle.Round(gdata.VisibleClipBounds);
            Bitmap bmp = new Bitmap(rect.Width, rect.Height);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(0, 0, 0, 0, rect.Size);
                IntPtr hdc = g.GetHdc();
                PrintWindow(TrgwHnd, hdc, 0x2);
                g.ReleaseHdc(hdc);
            }
            bmp.Save(@".\sample.bmp");
            bmp.Dispose();
            bmp = null;
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            textBox6.Text = "(" + Cursor.Position.X + ", " + Cursor.Position.Y + " )";
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            for(int i = 0; i < 2; i++)
            {
                if (threads[i] != null)
                {
                    threadEnabled[i] = false;
                    threads[i].Join();
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
        // 만약, int selected 가 0일 경우, 동작하지 않는다.
        // 그 외의 숫자일 경우, enum searchPoint에 따라서 맞게 작업한다.


        // 클릭 좌표, 인식점, 지연시간, 인식할 이미지를 수정하고 이를 imginfo.json 파일에 저장
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.E:
                    int idx = comboBox1.SelectedIndex;
                    if (imgResetFlag == 0)
                    {
                        button4.Text = "이미지 재설정";
                        double latency = Convert.ToDouble(textBox16.Text);
                        ImgInfoList[idx].ChangeImg(Cursor.Position.X - wndsizeX, Cursor.Position.Y - wndsizeY, latency);
                        int x, y;
                        x = ImgInfoList[idx].SearchPt.X;
                        y = ImgInfoList[idx].SearchPt.Y;
                        int sx, sy;
                        sx = ImgInfoList[idx].SearchSize.X;
                        sy = ImgInfoList[idx].SearchSize.Y;
                        int cx, cy;
                        cx = ImgInfoList[idx].ClickPt.X;
                        cy = ImgInfoList[idx].ClickPt.Y;
                        Bitmap bmpimage = (Bitmap)System.Drawing.Bitmap.FromFile(ImgInfoList[idx].ImgLoc);
                        DrawImageOnPointPB3(x, y, sx, sy, bmpimage);
                        DrawImageOnPointPB4(cx, cy, bmpimage);
                        bmpimage.Dispose();
                        bmpimage = null;
                    }
                    else if (imgResetFlag == 1)
                    {
                        button5.Text = "클릭좌표 설정";
                        int x, y;
                        x = Cursor.Position.X - wndsizeX;
                        y = Cursor.Position.Y - wndsizeY;
                        ImgInfoList[idx].SetClickPt(x, y);
                        Bitmap bmpimage = (Bitmap)System.Drawing.Bitmap.FromFile(ImgInfoList[idx].ImgLoc);
                        DrawImageOnPointPB4(ImgInfoList[idx].ClickPt.X, ImgInfoList[idx].ClickPt.Y, bmpimage);
                        bmpimage.Dispose();
                        bmpimage = null;
                    }
                    KeyPreview = false;
                    comboBox1.Enabled = true;
                    textBox16.Enabled = true;
                    WriteSearchInfo(1);

                    break;
            }
        }

        // 재스캔 키
        private void button4_Click(object sender, EventArgs e)
        {
            if(button4.Text == "(E)")
            {
                button4.Text = "인식점 설정";
                comboBox1.Enabled = true;
                textBox16.Enabled = true;
                KeyPreview = true;
                return;
            }

            if (comboBox1.SelectedIndex == -1) return;
            button4.Text = "(E)";
            imgResetFlag = 0;
            comboBox1.Enabled = false;
            textBox16.Enabled = false;
            KeyPreview = true;
        }
        // 클릭지점 재지정 키
        private void button5_Click(object sender, EventArgs e)
        {
            if (button5.Text == "(E)")
            {
                button5.Text = "클릭좌표 설정";
                comboBox1.Enabled = true;
                KeyPreview = true;
                return;
            }

            if (comboBox1.SelectedIndex == -1) return;
            button5.Text = "(E)";
            imgResetFlag = 1;
            comboBox1.Enabled = false;
            KeyPreview = true;


        }
        private void button6_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == -1) return;
            Graphics gdata = Graphics.FromHwnd(TrgwHnd);
            Rectangle rect = Rectangle.Round(gdata.VisibleClipBounds);
            Bitmap bmp = new Bitmap(rect.Width, rect.Height);
            int idx = comboBox1.SelectedIndex;
            int x, y;
            x = ImgInfoList[idx].SearchPt.X;
            y = ImgInfoList[idx].SearchPt.Y;
            int sx, sy;
            sx = ImgInfoList[idx].SearchSize.X;
            sy = ImgInfoList[idx].SearchSize.Y;
            int cx, cy;
            cx = ImgInfoList[idx].ClickPt.X;
            cy = ImgInfoList[idx].ClickPt.Y;

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(0, 0, 0, 0, rect.Size);
                IntPtr hdc = g.GetHdc();
                PrintWindow(TrgwHnd, hdc, 0x2);
                g.ReleaseHdc(hdc);
            }

            DrawImageOnPointPB3(x, y, sx, sy, bmp);
            DrawImageOnPointPB4(cx, cy, bmp);
            bmp.Save(ImgInfoList[idx].ImgLoc);
            bmp.Dispose();
        }
        // 콤보박스 변경키
        public void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = comboBox1.SelectedIndex;
            textBox16.Text = ImgInfoList[idx].click_latency.ToString();

            // 인식 좌표
            int x, y;
            x = ImgInfoList[idx].SearchPt.X;
            y = ImgInfoList[idx].SearchPt.Y;
            
            // 인식 사이즈 (고정값)
            int sx, sy;
            sx = ImgInfoList[idx].SearchSize.X;
            sy = ImgInfoList[idx].SearchSize.Y;

            // 클릭 좌표
            int cx, cy;
            cx = ImgInfoList[idx].ClickPt.X;
            cy = ImgInfoList[idx].ClickPt.Y;

            // 파일로 해당 이미지 불러오기
            System.Drawing.Bitmap SampleImg = (Bitmap)System.Drawing.Bitmap.FromFile(ImgInfoList[idx].ImgLoc);

            // 인식점 위치의 이미지
            DrawImageOnPointPB3(x, y, sx, sy, SampleImg);
            // 클릭점 위치의 이미지
            DrawImageOnPointPB4(cx, cy, SampleImg);

            // 파일로 불러온 이미지 리소스 해제 (필수!)
            SampleImg.Dispose();
        }

        // 인식점 대상 위치 이미지 미리보기
        public void DrawImageOnPointPB3(int x, int y, int sx, int sy, Bitmap bmp)
        {
            if (sx == 0 || sy == 0)
                return;
            Mat bmpAsMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
            Mat display = bmpAsMat.Clone(new Rect(x, y, sx, sy));
            pictureBox3.Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(display);
        }

        // 클릭점 대상 위치 미리보기
        public void DrawImageOnPointPB4(int x, int y,Bitmap bmp)
        {
            if (x < 15) x = 15;
            if (y < 15) y = 15;
            Mat bmpAsMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
            Mat display = bmpAsMat.Clone(new Rect(x - 15, y - 15, 30, 30));
            pictureBox4.Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(display);
        }
    }
}