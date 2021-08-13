using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;   // DllImport
using System.Diagnostics;               // Process
using System.Threading;                 // Thread.Sleep

namespace UnityEmbedWindowsFormsApp
{
    public partial class Form1 : Form
    {
        // window library 추가
        [DllImport("User32.dll")]
        static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool redraw);

        internal delegate int WindowEnumProc(IntPtr hwnd, IntPtr lparam);
        [DllImport("user32.dll")]
        internal static extern bool EnumChildWindows(IntPtr hwnd, WindowEnumProc func, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        // Unity 실행시킬 process 생성
        private Process process;
        private IntPtr unityHWND = IntPtr.Zero;

        // WM_활성화 메시지 참고
        // https://docs.microsoft.com/ko-kr/windows/win32/inputdev/wm-activate
        private const int WM_ACTIVATE = 0x0006;
        private readonly IntPtr WA_ACTIVE = new IntPtr(1);
        private readonly IntPtr WA_INACTIVE = new IntPtr(0);

        public Form1()
        {
            InitializeComponent();

            // Process 시작
            try
            {
                process = new Process();

                // Unity 실행파일은 winform 솔루션의 빌드 설정에 따라(Debug/Release) 맞는 폴더에 둘 것
                process.StartInfo.FileName = "UnityBuild\\New Unity Project.exe";

                // parentHWND: Embed the Windows Standalone application into another application. 
                // When you use this, you need to pass the parent application’s window handle (‘HWND’)
                // to the Windows Standalone application.
                // https://docs.unity3d.com/Manual/CommandLineArguments.html
                process.StartInfo.Arguments = "-parentHWND " + panel1.Handle.ToInt32() + " " + Environment.CommandLine;
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                process.WaitForInputIdle();

                // Unity가 로드되는 시간이 필요하다
                Thread.Sleep(3000);

                EnumChildWindows(panel1.Handle, WindowEnum, IntPtr.Zero);
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message + ".\nCheck if Container.exe is placed next to Child.exe.");
            }
        }

        private void ActivateUnityWindow()
        {
            SendMessage(unityHWND, WM_ACTIVATE, WA_ACTIVE, IntPtr.Zero);
        }

        private void DeActivateUnityWindow()
        {
            SendMessage(unityHWND, WM_ACTIVATE, WA_INACTIVE, IntPtr.Zero);
        }

        private int WindowEnum(IntPtr hwnd, IntPtr lparam)
        {
            unityHWND = hwnd;
            ActivateUnityWindow();
            return 0;
        }

        // Unity 종료안하고 Winform 종료하면 Unity에서 예외 터지므로 종료 처리를 해준다
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                process.CloseMainWindow();

                Thread.Sleep(1000);
                while (!process.HasExited)
                    process.Kill();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void panel1_Resize(object sender, EventArgs e)
        {
            MoveWindow(unityHWND, 0, 0, panel1.Width, panel1.Height, true);
            ActivateUnityWindow();
        }
    }
}
