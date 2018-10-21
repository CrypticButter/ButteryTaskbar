using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrypticButter.ButteryTaskbar
{
    class ComponentContainer : IContainer
    {
        ComponentCollection _components;

        public void Add(IComponent component) { }

        public void Add(IComponent component, string Name) { }

        public void Remove(IComponent component) { }

        public ComponentCollection Components
        {
            get { return _components; }
        }

        public void Dispose()
        {
            _components = null;
        }
    }

    class MainEntryClass
    {
        public static void Mains()
        {
            SomeClass sc = new SomeClass();
            Application.Run();
        }
    }

    class SomeClass
    {
        ComponentContainer container = new ComponentContainer();
        private System.Windows.Forms.NotifyIcon notifyIcon1;

        public SomeClass()
        {
            this.notifyIcon1 = new NotifyIcon(container);
            notifyIcon1.Visible = true;
            notifyIcon1.Icon = SystemIcons.Asterisk;
            notifyIcon1.ContextMenu = new ContextMenu(new MenuItem[] { new MenuItem("bob") });
        }
    }
}
