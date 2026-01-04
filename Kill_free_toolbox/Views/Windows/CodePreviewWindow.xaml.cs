using Kill_free_toolbox.Helper.Baijiahei;
using System.Windows;

namespace Kill_free_toolbox.Views.Windows
{
    public partial class CodePreviewWindow : Window
    {
        private readonly CodeRenderer renderer;
        public CodePreviewWindow(string dllName, string code)
        {
            InitializeComponent();
            this.Title = $"{dllName} - 源代码";
            renderer = new CodeRenderer(CodeBox);
            renderer.RenderCppCode(code);
        }
    }
}


