namespace CrypticButter.ButteryTaskbar
{
    using System.ComponentModel;

    /// <summary>
    /// Class to implement the IContainer interface, for containing the application's tray icon
    /// </summary>
    internal class ComponentContainer : IContainer
    {
        public ComponentCollection Components { get; private set; }

        public void Add(IComponent component)
        { }

        public void Add(IComponent component, string name)
        { }

        public void Remove(IComponent component)
        { }

        public void Dispose() => this.Components = null;
    }
}
