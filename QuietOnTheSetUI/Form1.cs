using Microsoft.Win32;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuietOnTheSetUI
{
    public partial class Form1 : Form
    {
        MMDeviceEnumerator MMDE = new MMDeviceEnumerator();
        MMDevice mmDevice;
        private bool _isLocked = false;
        private string _password;
        private int _maxVolume;
        private bool _exitAllowed = false;

        public Form1()
        {
            InitializeComponent();
            Bitmap applicationIcon = QuietOnTheSetUI.Properties.Resources.appicon;
            this.Icon = Icon.FromHandle(applicationIcon.GetHicon());
            mmDevice = MMDE.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            notifyIcon1.Icon = Icon.FromHandle(applicationIcon.GetHicon());
            volumeTrackBar.ValueChanged += VolumeTrackBar_ValueChanged;
            mmDevice.AudioEndpointVolume.OnVolumeNotification += AudioEndpointVolume_OnVolumeNotification;
            _maxVolume = Convert.ToInt16(Properties.Settings.Default["MaxVolume"]);
            volumeTrackBar.Value = _maxVolume;
            _isLocked = Convert.ToBoolean(Properties.Settings.Default["IsLocked"]);
            _password = Properties.Settings.Default["UnlockCode"].ToString();
            currentVolumeLabel.Text = Convert.ToInt16(mmDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100).ToString();
            notifyIcon1.BalloonTipTitle = $"Quiet on the Set";
            if (_isLocked)
            {
                LockVolume(true);
            }
            else
            {
                UnlockVolume();
            }

            this.FormClosing += Form1_FormClosing;
            this.Resize += Form1_Resize;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == this.WindowState)
            {
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(500);
                this.Hide();
            }
            else if (FormWindowState.Normal == this.WindowState)
            {
                notifyIcon1.Visible = false;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_exitAllowed == false)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                Hide();
            }
        }

        internal void LockVolume (bool initializing=false)
        {
            _isLocked = true;
            lockButton.Text = "Unlock";
            volumeTrackBar.Enabled = false;
            if (!initializing)
            {
                _maxVolume = volumeTrackBar.Value;
                Properties.Settings.Default["MaxVolume"] = _maxVolume.ToString();
                Properties.Settings.Default["IsLocked"] = true;
                Properties.Settings.Default["UnlockCode"] = passwordTextBox.Text;
                Properties.Settings.Default.Save();
            }
            _password = passwordTextBox.Text;
            passwordTextBox.Text = string.Empty;
            confirmPasswordTextBox.Text = string.Empty;
            notifyIcon1.BalloonTipText = $"Maximum volume locked at {volumeTrackBar.Value}";
            if (_password.Length > 0) { lockButton.Enabled = false; }
            exitButton.Visible = false;
            SetMaxVolume();
        }
        internal void UnlockVolume()
        {
            _isLocked = false;
            lockButton.Text = "Lock";
            volumeTrackBar.Enabled = true;
            Properties.Settings.Default["IsLocked"] = false;
            Properties.Settings.Default["UnlockCode"] = string.Empty;
            Properties.Settings.Default.Save();
            passwordTextBox.Text = string.Empty;
            confirmPasswordTextBox.Text = string.Empty;
            notifyIcon1.BalloonTipText = $"No maximum volume is current set";
            exitButton.Visible = true;
            _password = string.Empty;
        }

        private void SetMaxVolume()
        {
            if (mmDevice.AudioEndpointVolume.MasterVolumeLevelScalar > (_maxVolume / 100f))
            {
                mmDevice.AudioEndpointVolume.MasterVolumeLevelScalar = _maxVolume / 100f;
            }
        }

        private void AudioEndpointVolume_OnVolumeNotification(AudioVolumeNotificationData data)
        {
            var newVolume = Convert.ToInt16(data.MasterVolume * 100);
            if (_isLocked && newVolume >  _maxVolume)
            {
                SetMaxVolume();
            }
            if (currentVolumeLabel.InvokeRequired)
            {
                currentVolumeLabel.Invoke(new MethodInvoker(delegate { currentVolumeLabel.Text = newVolume.ToString() ; }));
            }
        }

        private void VolumeTrackBar_ValueChanged(object sender, EventArgs e)
        {
            maxVolumeLabel.Text = volumeTrackBar.Value.ToString();
        }

        private void volumeTrackBar_Scroll(object sender, EventArgs e)
        {
            var trackBar = (TrackBar)sender;
            maxVolumeLabel.Text = trackBar.Value.ToString();
        }

        private void lockButton_Click(object sender, EventArgs e)
        {
            if (_isLocked)
            {
                UnlockVolume();
            }
            else
            {
                LockVolume();
            }
        }

        private void passwordTextBox_TextChanged(object sender, EventArgs e)
        {
            ValidatePasswords();
        }

        private void confirmPasswordTextBox_TextChanged(object sender, EventArgs e)
        {
            ValidatePasswords();
        }

        internal void ValidatePasswords()
        {
            if (_isLocked)
            {
                lockButton.Enabled = passwordTextBox.Text.Equals(confirmPasswordTextBox.Text) && passwordTextBox.Text.Equals(_password);
            }
            else
            {
                lockButton.Enabled = passwordTextBox.Text.Equals(confirmPasswordTextBox.Text);
            }
        }

        private void showPasswordCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            var isChecked = ((CheckBox)sender).Checked;
            passwordTextBox.UseSystemPasswordChar = !isChecked;
            confirmPasswordTextBox.UseSystemPasswordChar = !isChecked;
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            Form1 frm = new Form1();
            frm.Show();
            MaxmizedFromTray();
        }
        private void MinimzedTray()
        {
            notifyIcon1.Visible = true;
            notifyIcon1.Icon = SystemIcons.Application;

            notifyIcon1.BalloonTipText = "Minimized";
            notifyIcon1.BalloonTipTitle = "Your Application is Running in BackGround";
            notifyIcon1.ShowBalloonTip(500);
        }

        private void MaxmizedFromTray()
        {
            notifyIcon1.Visible = true;
            notifyIcon1.BalloonTipText = "Maximized";
            notifyIcon1.BalloonTipTitle = "Application is Running in Foreground";
            notifyIcon1.ShowBalloonTip(500);
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            var response = MessageBox.Show("This will completely shut down the volume control so the . Are you sure you want to exit?", "Warning", MessageBoxButtons.YesNo);
            if (response == DialogResult.Yes)
            {
                _exitAllowed = true;
                Application.Exit();
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (checkBox1.Checked)
            {
                rk.SetValue("QuietOnTheSet", Application.ExecutablePath.ToString());
            }
            else
            {
                rk.DeleteValue("QuietOnTheSet", false);
            }
        }
    }
}
