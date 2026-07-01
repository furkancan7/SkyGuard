using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Runtime.InteropServices;

namespace osmanlicaklavye
{
    public sealed partial class MainWindow : Window
    {
        // Klavye düzeni (Harfler)
        private readonly string[][] _layout = new string[][] {
            new string[] { "١", "٢", "٣", "٤", "٥", "٦", "٧", "٨", "٩", "٠" },
            new string[] { "ض", "ص", "ث", "ق", "ف", "غ", "ع", "ه", "خ", "ح", "ج", "چ" },
            new string[] { "ش", "س", "ي", "ب", "ل", "ا", "ت", "ن", "م", "ك", "گ", "ڭ" },
            new string[] { "ئ", "ء", "ؤ", "ر", "لا", "ى", "ة", "و", "z", "ژ", "د", "ذ" }
        };

        public MainWindow()
        {
            // Siyah ekranı çözen temel komut
            this.InitializeComponent();

            // Pencereyi her zaman üstte tutma özelliği
            this.AppWindow.SetPresentKind(Microsoft.UI.Windowing.AppWindowPresentKind.CompactOverlay);

            BuildKeyboard();
        }

        private void BuildKeyboard()
        {
            foreach (var row in _layout)
            {
                var rowPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 4 };
                foreach (var key in row)
                {
                    var btn = new Button { Content = key, Width = 42, Height = 42, FontSize = 18 };
                    btn.Click += (s, e) => SendCharacter(key);
                    rowPanel.Children.Add(btn);
                }
                KeyboardGrid.Items.Add(rowPanel);
            }
        }

        private void SendCharacter(string text)
        {
            foreach (char c in text) InputSimulator.SendChar(c);
        }

        private void OnSpaceClick(object sender, RoutedEventArgs e) => SendCharacter(" ");
        private void OnBackspaceClick(object sender, RoutedEventArgs e) => InputSimulator.SendVirtualKey(0x08);
        private void OnEnterClick(object sender, RoutedEventArgs e) => InputSimulator.SendVirtualKey(0x0D);
    }

    // Sisteme tuş gönderen yardımcı sınıf
    public static class InputSimulator
    {
        [DllImport("user32.dll")] private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
        [StructLayout(LayoutKind.Sequential)] struct INPUT { public uint type; public INPUTUNION union; }
        [StructLayout(LayoutKind.Explicit)] struct INPUTUNION { [FieldOffset(0)] public KEYBDINPUT ki; }
        [StructLayout(LayoutKind.Sequential)] struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

        public static void SendChar(char c)
        {
            INPUT[] inputs = new INPUT[2];
            inputs[0].type = 1; inputs[0].union.ki.wScan = c; inputs[0].union.ki.dwFlags = 0x0004;
            inputs[1].type = 1; inputs[1].union.ki.wScan = c; inputs[1].union.ki.dwFlags = 0x0004 | 0x0002;
            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void SendVirtualKey(ushort vk)
        {
            INPUT[] inputs = new INPUT[2];
            inputs[0].type = 1; inputs[0].union.ki.wVk = vk;
            inputs[1].type = 1; inputs[1].union.ki.wVk = vk; inputs[1].union.ki.dwFlags = 0x0002;
            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}