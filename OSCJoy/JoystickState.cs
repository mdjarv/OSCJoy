using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSCJoy
{
    public class JoystickState : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void Notify(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion INotifyPropertyChanged

        private float axisX;
        private float axisY;
        public bool button1;

        public float AxisX
        {
            get
            {
                return axisX;
            }

            set
            {
                axisX = value;
                Notify("AxisX");
            }
        }

        public float AxisY
        {
            get
            {
                return axisY;
            }

            set
            {
                axisY = value;
                Notify("AxisY");
            }
        }

    }
}
