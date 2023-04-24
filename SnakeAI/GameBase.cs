using Autofac;
using Microsoft.Xna.Framework;

namespace SnakeAI
{
    public abstract class GameBase : Game
    {
        // ReSharper disable once NotAccessedField.Local
        protected GraphicsDeviceManager GraphicsDeviceManager { get; }
        protected IContainer Container { get; private set; }

        public int Width { get; }
        public int Height { get; }

        protected GameBase(int width = 1280, int height = 720)
        {
            Width = width;
            Height = height;
            GraphicsDeviceManager = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = width,
                PreferredBackBufferHeight = height
            };
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            var containerBuilder = new ContainerBuilder();

            RegisterDependencies(containerBuilder);
            Container = containerBuilder.Build();

            base.Initialize();
        }

        protected abstract void RegisterDependencies(ContainerBuilder builder);
    }
}