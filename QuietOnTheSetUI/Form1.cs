using Microsoft.Win32;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
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
            //            Bitmap applicationIcon = QuietOnTheSetUI.Properties.Resources.appicon;

            try
            {
                checkBox1.Checked = Convert.ToBoolean(Properties.Settings.Default["StartAutomatically"]);
                checkBox2.Checked = Convert.ToBoolean(Properties.Settings.Default["StartMinimized"]);
                if (checkBox2.Checked)
                {
                    //  Hides the app completely
                    Form1_FormClosing(null, new FormClosingEventArgs(new CloseReason(), true));

                    //  The volume is automatically locked if the app is minimized 
                    Properties.Settings.Default["IsLocked"] = true;
                    Properties.Settings.Default.Save();
                }
            }
            catch (Exception)
            {
                checkBox1.Checked = false;
                checkBox2.Checked = false;
            }

            this.Icon = QuietOnTheSetUI.Properties.Resources.appicon;
            mmDevice = MMDE.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            notifyIcon1.Icon = QuietOnTheSetUI.Properties.Resources.appicon;
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

            UpdateFooter();
        }

        private void UpdateFooter()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            DateTime buildDate = new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime;

            footerLabel.Text = $"v{version} was built {buildDate.ToString("g")}";
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == this.WindowState)
            {
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;  // Hide QOTS from alt+tab
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(500);
                this.Hide();
            }
            else if (FormWindowState.Normal == this.WindowState)
            {
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
                notifyIcon1.Visible = false;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_exitAllowed == false)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
            }
        }

        internal void LockVolume(bool initializing = false)
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
            notifyIcon1.BalloonTipText = _balloonTipText;
            notifyIcon1.Text = _balloonTipText;
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
            notifyIcon1.BalloonTipText = _balloonTipText;
            notifyIcon1.Text = _balloonTipText;
            exitButton.Visible = true;
            _password = string.Empty;
        }

        private string _balloonTipText
        {
            get
            {
                if (_isLocked)
                {
                    return $"Maximum volume locked at {volumeTrackBar.Value}";
                }
                else
                {
                    return $"No maximum volume is currently set";
                }
            }
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
            if (_isLocked && newVolume > _maxVolume)
            {
                SetMaxVolume();
            }
            if (currentVolumeLabel.InvokeRequired)
            {
                currentVolumeLabel.Invoke(new MethodInvoker(delegate { currentVolumeLabel.Text = newVolume.ToString(); }));
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
            // Show must be called before setting WindowState,
            // otherwise the window loses its size and position
            this.Show();
            this.WindowState = FormWindowState.Normal;
            MaxmizedFromTray();
        }

        private void MaxmizedFromTray()
        {
            notifyIcon1.Visible = false;
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            var response = MessageBox.Show("This will completely shut down the volume control so users can set the volume as loud as they want. Are you sure you want to exit?", "Warning", MessageBoxButtons.YesNo);
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

            Properties.Settings.Default["StartAutomatically"] = checkBox1.Checked;
            Properties.Settings.Default.Save();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default["StartMinimized"] = checkBox2.Checked;
            Properties.Settings.Default.Save();
        }
    }
}
