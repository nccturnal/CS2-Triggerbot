using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace cs2_triggerbot
{
    class Program
    {
        // Windows API imports
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(Keys vKey);

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        // Mouse event flags
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;

        // Offsets
        const int dwEntityList = 0x1D16758;
        const int dwLocalPlayerPawn = 0x1BF1FA0;
        const int m_iIDEntIndex = 0x3EDC;
        const int m_iTeamNum = 0x3EB;
        const int m_iHealth = 0x34C;

        // Trigger key
        const Keys triggerKey = Keys.ShiftKey;

        static void Main(string[] args)
        {
            Console.WriteLine($"TriggerBot started.");
            Console.WriteLine($"Trigger key: {triggerKey}");

            Process process = null;
            IntPtr clientModule = IntPtr.Zero;

            try
            {
                // Find CS2 process
                Process[] processes = Process.GetProcessesByName("cs2");
                if (processes.Length == 0)
                {
                    Console.Clear();
                    Console.WriteLine("open cs 2");
                    Console.ReadKey();
                    return;
                }

                process = processes[0];

                // Get client.dll module
                foreach (ProcessModule module in process.Modules)
                {
                    if (module.ModuleName == "client.dll")
                    {
                        clientModule = module.BaseAddress;
                        break;
                    }
                }

                if (clientModule == IntPtr.Zero)
                {
                    Console.WriteLine("Failed to find client.dll module");
                    Console.ReadKey();
                    return;
                }

                Random random = new Random();

                while (true)
                {
                    try
                    {
                        // Check if CS2 is the active window
                        if (GetActiveWindowTitle() != "Counter-Strike 2")
                        {
                            Thread.Sleep(100);
                            continue;
                        }

                        // Check if trigger key is pressed
                        if (IsKeyPressed(triggerKey))
                        {
                            long player = ReadLong(process, clientModule + dwLocalPlayerPawn);
                            int entityId = ReadInt(process, (IntPtr)player + m_iIDEntIndex);

                            if (entityId > 0)
                            {
                                long entList = ReadLong(process, clientModule + dwEntityList);
                                long entEntry = ReadLong(process, (IntPtr)(entList + 0x8 * (entityId >> 9) + 0x10));
                                long entity = ReadLong(process, (IntPtr)(entEntry + 120 * (entityId & 0x1FF)));

                                int entityTeam = ReadInt(process, (IntPtr)(entity + m_iTeamNum));
                                int playerTeam = ReadInt(process, (IntPtr)(player + m_iTeamNum));

                                if (entityTeam != playerTeam)
                                {
                                    int entityHp = ReadInt(process, (IntPtr)(entity + m_iHealth));

                                    if (entityHp > 0)
                                    {
                                        // Random delay before shooting
                                        Thread.Sleep(random.Next(10, 30));

                                        // Press mouse button
                                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                                        Thread.Sleep(random.Next(10, 50));
                                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                                    }
                                }
                            }

                            Thread.Sleep(30);
                        }
                        else
                        {
                            Thread.Sleep(100);
                        }
                    }
                    catch (Exception)
                    {
                        // Continue on error
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.ReadKey();
            }
        }

        static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            System.Text.StringBuilder buff = new System.Text.StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, buff, nChars) > 0)
            {
                return buff.ToString();
            }
            return string.Empty;
        }

        static bool IsKeyPressed(Keys key)
        {
            return (GetAsyncKeyState(key) & 0x8000) != 0;
        }

        static int ReadInt(Process process, IntPtr address)
        {
            byte[] buffer = new byte[4];
            IntPtr bytesRead;
            ReadProcessMemory(process.Handle, address, buffer, buffer.Length, out bytesRead);
            return BitConverter.ToInt32(buffer, 0);
        }

        static long ReadLong(Process process, IntPtr address)
        {
            byte[] buffer = new byte[8];
            IntPtr bytesRead;
            ReadProcessMemory(process.Handle, address, buffer, buffer.Length, out bytesRead);
            return BitConverter.ToInt64(buffer, 0);
        }

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);
    }

}
