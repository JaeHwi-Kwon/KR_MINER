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
        class AppInfo_NOX {
            public const int wndSizeX = 2;
            public const int wndSizeY = 48;
            public static KeyValuePair<string, string> WndParent = new KeyValuePair<string, string>("Qt5QWindowIcon", null);
            public static KeyValuePair<string, string> WndChild = new KeyValuePair<string, string>(null, "ScreenBoardClassWindow");
            public static KeyValuePair<string, string> TargetWnd = new KeyValuePair<string, string>(null, "sub");
        }
        

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
        public double imgacr = 80.0;
        public double latencytweaker = 1.0;
        public int generalgoldcycle = 6;
        public double goldbuttonlatency = 1.0;
        public int gamestoppedlatency = 20;


        public Form1()
        {
            InitializeComponent();
            for(int i = 0; i < 2; ++i)
            {
                threadEnabled[i] = false;
                threads[i] = null;
            }
            int app = WhichAppPlayer();
            
            FindWnd(app);
            gachaCnt = 0;

            LoadPreset();
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
                goldbuttonlatency = (double)json["goldbuttonlatency"];
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

            textBox11.Text = goldbuttonlatency.ToString();
            textBox12.Text = generalgoldcycle.ToString();
            textBox13.Text = latencytweaker.ToString();
            textBox14.Text = gamestoppedlatency.ToString();
        }

        // 0이면 초기값 입력, 1이면 현재 설정값 입력
        private void WritePreset(int flag)
        {
            if (flag == 0)
            {
                JObject preset = new JObject(
                    new JProperty("gamestoppedlatency", 20),
                    new JProperty("goldbuttonlatency", 1.0),
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
                    new JProperty("goldbuttonlatency", goldbuttonlatency),
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
            else if (FindWindow(AppInfo_NOX.WndParent.Key, AppInfo_NOX.WndParent.Value) != IntPtr.Zero)
            {
                return (int)AppPlayer.NOX;
            }

            return -1;
        }

        public OpenCvSharp.Point searchImageLocation(Bitmap screen_img, int x,int y, int width, int height, Bitmap where)
        {
            double minval, maxval = 0;
            OpenCvSharp.Point minloc, maxloc;
            using (Mat imgMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(screen_img))
            using (Mat compMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(where))
            {
                var imgMat_conv = imgMat.Clone(new Rect(x, y, height, width));
                using (Mat res = imgMat_conv.MatchTemplate(compMat, TemplateMatchModes.CCoeffNormed))
                {
                    Cv2.MinMaxLoc(res, out minval, out maxval, out minloc, out maxloc);
                    if(maxval == 1)
                    {
                        return minloc;
                    }
                    else
                    {
                        return new OpenCvSharp.Point(-1, -1);
                    }
                }
            }
        }

        // img를 비교대상 comparison과 비교한다.
        // 비교 범위는 x,y 좌표상의 height,width 크기만큼 잘라내어 비교한다.
        // 결과는 그 유사도를 출력한다.
        public double ImgCompare(Bitmap img, Bitmap comparison, int x, int y, int height, int width)
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
                        //textBox1.Text += "일치율 : " + maxval * 100 + "% \r\n";
                        var newpic_img = imgMat_conv;
                        var newpic_comp = compMat_conv;
                        pictureBox1.Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(newpic_img);
                        pictureBox2.Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(newpic_comp);
                        textBox15.AppendText("일치율 :" + maxval * 100 + "% \r\n");
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
                case (int)AppPlayer.NOX:
                    wndsizeX = AppInfo_NOX.wndSizeX;
                    wndsizeY = AppInfo_NOX.wndSizeY;
                    textBox9.Text = "You're Running NOX Player";
                    ParentwHnd = FindWindow(AppInfo_NOX.WndParent.Key, AppInfo_NOX.WndParent.Value);
                    ChildwHnd = FindWindowEx(ParentwHnd, IntPtr.Zero, AppInfo_NOX.WndChild.Key, AppInfo_NOX.WndChild.Value);
                    TrgwHnd = FindWindowEx(ChildwHnd, IntPtr.Zero, AppInfo_NOX.TargetWnd.Key, AppInfo_NOX.TargetWnd.Value);
                    SetWindowPos((int)ParentwHnd, 0, 0, 0, 1280 + wndsizeX, 720 + wndsizeY, 0x10);
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

            goldbuttonlatency = Convert.ToDouble(textBox11.Text);
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
            textBox11.ReadOnly = true;
            textBox12.ReadOnly = true;
            textBox13.ReadOnly = true;
            textBox14.ReadOnly = true;

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
            textBox11.ReadOnly = false;
            textBox12.ReadOnly = false;
            textBox13.ReadOnly = false;
            textBox14.ReadOnly = false;
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
                
                if (ImgCompare(screenshot, screenshotcomp, 0, 0, 800, 600) >= 0.99)
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
                    screenshotcomp = screenshot;
                    frzCnt = 0;
                }

            }
        }

        // 1번 쓰레드
        private void Start_Reboot_Seq()
        {
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

            while (ImgCompare(screenshot, Resource1.kingsraid_main,135,646, 30, 20) < imgacr / 100)
            {
                MouseClick(650 + 15, 577 + 15);
                if (threadEnabled[1] == false)
                {
                    isBusy = false; return;
                }
                Thread.Sleep(1000);
            }
            latency = 4000 * latencytweaker;
            Thread.Sleep((int)latency);
            MouseClick(135 + 15, 646 + 10);

            while (ImgCompare(screenshot, Resource1.Inventory, 820, 11, 30, 30) < imgacr / 100)
            {
                Thread.Sleep(100);
                if (threadEnabled[1] == false)
                {
                    isBusy = false; return;
                }
            }
            //textBox1.Text += "매크로 시퀀스 5 실행 \r\n";
            latency = 2000 * latencytweaker;
            Thread.Sleep((int)latency);
            MouseClick(820 + 15, 11 + 15);

            while (ImgCompare(screenshot, Resource1.Store_Gold, 55, 626, 30, 20) < imgacr / 100)
            {
                Thread.Sleep(100);
                if (threadEnabled[1] == false)
                {
                    isBusy = false; return;
                }
            }
            //textBox1.Text += "매크로 시퀀스 6 실행 \r\n";
            latency = 1000 * latencytweaker;
            Thread.Sleep((int)latency);
            MouseClick(55 + 15, 626 + 10);

            while (ImgCompare(screenshot, Resource1.Store_Gacha, 25, 486, 80, 20) < imgacr / 100)
            {
                Thread.Sleep(100);
                if (threadEnabled[1] == false)
                {
                    isBusy = false; return;
                }
            }
            //textBox1.Text += "매크로 시퀀스 7 실행 \r\n";
            Thread.Sleep((int)latency);
            MouseClick(25 + 40, 486 + 10);

            while (ImgCompare(screenshot, Resource1.Store_Gacha_Normal, 325, 510, 100, 100) < imgacr / 100)
            {
                Thread.Sleep(100);
                if (threadEnabled[1] == false)
                {
                    isBusy = false; return;
                }
            }
            //textBox1.Text += "매크로 시퀀스 8 실행 \r\n";
            Thread.Sleep((int)latency);
            MouseClick(325 + 50, 510 + 50);

            isBusy = false;
        }

        public void Start_Gacha_Seq()
        {
            double latency;
            if (ImgCompare(screenshot, Resource1.Gold_Empty_Message,
                445, 510, 30, 30) > imgacr / 100)
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
                textBox11.ReadOnly = false;
                textBox12.ReadOnly = false;
                textBox13.ReadOnly = false;
                textBox14.ReadOnly = false;
                textBox1.AppendText("골드가 다 소진되었습니다.\r\n");

                return;
            }
            
            if (ImgCompare(screenshot, Resource1.General_Gacha_Full,
                575, 510, 120, 30) > imgacr / 100)
            {
                if (gachaCnt > 0) gachaCnt -= 1;
                textBox1.AppendText("가방꽉참확인 인식. \r\n");
                latency = 500 * latencytweaker;
                Thread.Sleep((int)latency);
                MouseClick(575 + 60, 510 + 15);

                Thread t1 = new Thread(Start_BackPack_Seq);
                threads[0] = t1;
                threadEnabled[0] = true;
                isBusy = true;
                t1.Start();
            }

            else if (ImgCompare(screenshot, Resource1.Store_Gacha_Normal_Goldx10,
                975, 646, 30, 30) >= imgacr/100)
            {
                if (cnt >= generalgoldcycle*10)
                {
                    latency = goldbuttonlatency * 1000;
                    Thread.Sleep((int)latency);
                    //textBox1.Text += "소환버튼 인식 \r\n";
                    MouseClick(975 + 15, 646 + 15);
                    gachaCnt += 1;
                    textBox2.Text = "누적횟수 : " + gachaCnt;
                    cnt = 0;
                }
            }

            if(cnt == 30 || frzCnt > 3)
            {
                if (ImgCompare(screenshot, Resource1.Store_Gacha_Normal_Goldx10,
                1180, 646, 30, 30) >= imgacr / 100)
                {
                    if (gachaCnt > 0) gachaCnt -= 1;
                }
            }
        }

        // 0번 쓰레드
        public void Start_BackPack_Seq()
        {
            double latency;
            while (ImgCompare(screenshot,Resource1.Inventory, 1080, 610, 30, 30) < imgacr / 100) 
            { 
                Thread.Sleep(100);
                if (threadEnabled[0] == false)
                {
                    isBusy = false; return;
                }
            }
            //textBox1.Text += "매크로 시퀀스 1 실행 \r\n";
            latency = 2500 * latencytweaker;
            Thread.Sleep((int)latency);
            MouseClick(1080 + 15, 610 + 15);

            while(ImgCompare(screenshot,Resource1.Inventory_sell, 895, 610, 100, 30) < imgacr / 100)
            {
                Thread.Sleep(100);
                if (threadEnabled[0] == false)
                {
                    isBusy = false; return;
                }
            }
            //textBox1.Text += "매크로 시퀀스 2 실행 \r\n";
            latency = 1000 * latencytweaker;
            Thread.Sleep((int)latency);
            MouseClick(895 + 50, 610 + 15);

            while(ImgCompare(screenshot,Resource1.Inventory_SellAll,775, 590, 30, 30) < imgacr / 100)
            {
                Thread.Sleep(100);
                if (threadEnabled[0] == false)
                {
                    isBusy = false; return;
                }
            }
            //textBox1.Text += "매크로 시퀀스 3 실행 \r\n";
            Thread.Sleep((int)latency);
            MouseClick(775 + 15, 590 + 15);

            while(ImgCompare(screenshot, Resource1.Inventory_SellAll_Confirm,735, 516, 30, 30) < imgacr / 100)
            {
                Thread.Sleep(100);
                if (threadEnabled[0] == false)
                {
                    isBusy = false; return;
                }
            }
            //textBox1.Text += "매크로 시퀀스 4 실행 \r\n";
            Thread.Sleep((int)latency);
            MouseClick(735 + 15, 516 + 15);

            while(ImgCompare(screenshot, Resource1.Inventory,100,0, 60, 40) < (imgacr) / 100)
            {
                Thread.Sleep(100);
                if (threadEnabled[0] == false)
                {
                    isBusy = false; return;
                }
            }
            //textBox1.Text += "매크로 시퀀스 5 실행 \r\n";
            latency = 2500 * latencytweaker;
            Thread.Sleep((int)latency);
            MouseClick(100 + 30, 0 + 20);

            while (ImgCompare(screenshot, Resource1.Store_Gacha_Normal, 325, 510, 100, 100) < imgacr / 100)
            {
                if(ImgCompare(screenshot, Resource1.Inventory, 105 - wndsizeX, 50 - wndsizeY, 60, 40) > (imgacr) / 100)
                {
                    Thread.Sleep(1000);
                    MouseClick(100 + 30, 0 + 20);
                }
                Thread.Sleep(100);
                if (threadEnabled[0] == false)
                {
                    isBusy = false; 
                    return;
                }
            }

            latency = 2500 * latencytweaker;
            Thread.Sleep((int)latency);
            MouseClick(325 + 50, 510 + 50);
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
    }
}
