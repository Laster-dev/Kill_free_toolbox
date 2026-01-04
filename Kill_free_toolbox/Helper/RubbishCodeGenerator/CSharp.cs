using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kill_free_toolbox.Helper.RubbishCodeGenerator
{

    public class RubbishCode_CSharp
    {
        static string filePath = "/RubbishCode/RubbishCode.cs";
        static string methodNamePrefix = "AutoGBCode";
        static string className = "AutoCreateRubbish";
        static List<string> usedMethodName = new List<string>();

        static int methodCount = 1;
        static int methodLineCount = 10;
        /// <summary>
        /// 垃圾代码类中的控制变量，用于控制垃圾方法的具体行为
        /// </summary>
        static string forLoopCycleCount = "forLoopCycleCount";
        static string whileLoopCycleCount = "whileLoopCycleCount";
        static string openLoop = "openForLoop";
        static string openWhile = "openWhile";
        static string openIfElse = "openIfElse";

        public static void CreateCode()
        {

            string realPath = Path.Combine(Environment.CurrentDirectory, filePath);

            var fs = new FileStream(realPath, FileMode.OpenOrCreate);

            if (!fs.CanWrite)
            {
                Console.WriteLine("无法写入文件");
                return;
            }

            string data = CreateClass(false);

            var bytes = Encoding.UTF8.GetBytes(data);
            Console.WriteLine("class总长度：" + bytes.Length);

            fs.Write(bytes, 0, bytes.Length);


            fs.Flush();
            fs.Close();
        }

        static string CreateClass(bool implementMono)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(CreateUsing());
            var str = CreateClassHead(false, className);
            sb.Append(str);
            sb.Append(CreateControlVariables());

            for (int i = 0; i < methodCount; i++)
            {
                int j = new Random().Next(20, 50);
                bool k = i % 2 == 0;
                string returnValue = GetReturnValue();
                sb.Append(CreateMethod(k, returnValue, methodNamePrefix, methodLineCount));

            }

            sb.Append("\n}");

            return sb.ToString();
        }

        private static string GetReturnValue()
        {
            int i = new Random().Next(1, 6);

            switch (i)
            {
                case 1:
                    return "int";
                case 2:
                    return "string";
                case 3:
                    return "long";
                case 4:
                    return "object";
                case 5:
                    return "double";
                default:
                    return "int";
            }
        }

        static string CreateUsing()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("using System;\nusing System.Collections.Generic;\nusing System.Text;");

            return sb.ToString();
        }

        static string CreateClassHead(bool implementMono, string className)
        {
            string str = implementMono ? ":MonoBehaviour" : "";
            return "\npublic static class " + className + str + "\n{";
        }

        /// <summary>
        /// 创建类的控制类变量，包含：
        /// 1.for循环次数
        /// 2.是否开启循环
        /// 3.是否开启switch语句
        /// 4.是否开启判断语句
        /// </summary>
        /// <returns></returns>
        static string CreateControlVariables()
        {
            string _forLoop = "\n\tpublic static int " + forLoopCycleCount + " = 1000;";
            string _openLoop = "\n\tpublic static bool " + openLoop + " = true;";
            string _openWhile = "\n\tpublic static bool " + openWhile + " = true;";
            string _openIfElse = "\n\tpublic static bool " + openIfElse + " = true;";
            string _whileLoop = "\n\tpublic static int " + whileLoopCycleCount + " = 1000;";
            return _forLoop + _openLoop + _openWhile + _openIfElse + _whileLoop;
        }

        /// <summary>
        /// 创建一个随机函数
        /// </summary>
        /// <param name="hasReturnValue"></param>
        /// <param name="methodNamePrefix"></param>
        /// <returns></returns>
        static string CreateMethod(bool hasReturnValue, string returnValueType, string methodNamePrefix, int totalLine)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(CreateMethodHead(hasReturnValue, methodNamePrefix, returnValueType));

            sb.Append(CreateMethodBody(totalLine, hasReturnValue, returnValueType));

            sb.Append("\n\t}");

            return sb.ToString();
        }

        /// <summary>
        /// 创建函数头部，格式为 public 返回值 函数名(){},需要注意这些函数全部没有参数名，方便调用
        /// </summary>
        /// <param name="hasReturnValue"> 是否有返回值</param>
        /// <param name="methodNamePrefix">如果有返回值 返回值类型</param>
        /// <returns></returns>
        static string CreateMethodHead(bool hasReturnValue, string methodNamePrefix, string returnType)
        {
            var methodName = methodNamePrefix + RandomMethodName();
            var returnStr = hasReturnValue ? returnType : "void";
            return "\n\n\tpublic " + returnStr + " " + methodName + "()\n\t{";
        }

        /// <summary>
        /// 创建函数体，为包含在函数{}内部的代码，由几部分组拼而成
        /// </summary>
        /// <param name="needToRun">需不需函数运行，如果不需要，直接return</param>
        /// <param name="totalLine">总共需要多少行代码</param>
        /// <param name="hasReturnValue">是否有返回值</param>
        /// <param name="ReturnValueType">如果有返回值，返回值的类型</param>
        /// <returns></returns>
        static string CreateMethodBody(int totalLine, bool hasReturnValue, string ReturnValueType)
        {
            string returnStatement = CreateReturnStatement(ReturnValueType, 2);//返回语句

            if (totalLine < 10)
            {
                totalLine = 11;
            }
            int totalCount = new Random().Next(10, totalLine);

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < totalCount; i++)
            {
                int j = new Random().Next(0, 3);
                int k = new Random().Next(0, 3);

                switch (j)
                {
                    case 0:
                        sb.Append(CreateForLoop(CreateSingleRandomStatement(k)));
                        break;
                    case 1:
                        int m = new Random().Next(0, 3);
                        sb.Append(CreateIfElse(CreateSingleRandomStatement(k), CreateSingleRandomStatement(m)));
                        break;
                    case 2:
                        sb.Append(CreateWhile(CreateSingleRandomStatement(k)));
                        break;
                    default:
                        break;
                }
            }

            sb.Append(returnStatement);

            return sb.ToString();


        }

        /// <summary>
        /// 创建For循环，其中循环次数为类的静态全局变量控制
        /// int forLoops：控制循环次数
        /// bool openLoop: 是否开启循环
        /// </summary>
        /// <param name="statementInForLoop">要放入for循环的具体语句</param>
        /// <returns></returns>
        static string CreateForLoop(string statementInForLoop)
        {
            return "\n\n\t\tif(" + openLoop + ")\n\t\t{\n\t\t\tfor(int i = 0;i<" + forLoopCycleCount + ";i++)\n\t\t\t{\n\t\t\t\t" + statementInForLoop + "\n\t\t\t}\n\t\t}";
        }

        /// <summary>
        /// 创建 if-else判断
        /// </summary>
        /// <param name="ifString">if语句里面要执行的东西</param>
        /// <param name="elseString">else语句里面要执行的东西</param>
        /// <returns></returns>
        static string CreateIfElse(string ifString, string elseString)
        {
            return "\n\n\t\tif(" + openIfElse + ")\n\t\t{\n\t\t\t" + ifString + "\n\t\t}\n\t\telse\n\t\t{\n\t\t\t" + elseString + "\n\t\t}";
        }


        /// <summary>
        /// 创建while循环
        /// </summary>
        /// <param name="whileStr">while循环中要执行的东西</param>
        /// <returns></returns>
        static string CreateWhile(string whileStr)
        {
            return "\n\n\t\tif(" + openWhile + ")\n\t\t{\n\t\t\tint i =0;\n\t\t\twhile(i<" + whileLoopCycleCount + ")\n\t\t\t{\n\t\t\t\t" + whileStr + "\n\t\t\t}\n\t\t}";
        }

        /// <summary>
        /// 创建返回语句
        /// </summary>
        /// <param name="returnValueType"></param>
        /// <param name="suojin"></param>
        /// <returns></returns>
        static string CreateReturnStatement(string returnValueType, int suojin)
        {
            return "\n" + GetSuoJin(suojin) + "return default(" + returnValueType + ");";
        }

        /// <summary>
        /// 获取缩进的字符串
        /// </summary>
        /// <param name="suojin"></param>
        /// <returns></returns>
        static string GetSuoJin(int suojin)
        {
            if (suojin <= 0)
            {
                return "";
            }


            string suojinstr = string.Empty;

            for (int i = 0; i < suojin; i++)
            {
                suojinstr += "\t";
            }

            return suojinstr;
        }

        /// <summary>
        /// 随机函数名字
        /// </summary>
        /// <returns></returns>
        static string RandomMethodName()
        {
            int methodLength = new Random().Next(5, 15);

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < methodLength; i++)
            {
                sb.Append(GetLetter(new Random().Next(1, 27)));
            }

            if (usedMethodName.Contains(sb.ToString()))
            {
                return RandomMethodName();
            }
            else
            {
                usedMethodName.Add(sb.ToString());
                return sb.ToString();
            }

        }

        static string CreateSingleRandomStatement(int index)
        {
            switch (index)
            {
                case 0:
                    return "int neverMSDFA = new Random().Next(0,100);";
                case 1:
                    return "Console.WriteLine(\"HELLO WORLD\");";
                case 2:
                    return "var str = \"Hello world\";";
                default:
                    return "";
            }

        }

        static string GetLetter(int index)
        {
            switch (index)
            {
                case 1:
                    return "A";
                case 2:
                    return "B";
                case 3:
                    return "C";
                case 4:
                    return "D";
                case 5:
                    return "E";
                case 6:
                    return "F";
                case 7:
                    return "G";
                case 8:
                    return "H";
                case 9:
                    return "I";
                case 10:
                    return "J";
                case 11:
                    return "K";
                case 12:
                    return "L";
                case 13:
                    return "M";
                case 14:
                    return "N";
                case 15:
                    return "O";
                case 16:
                    return "P";
                case 17:
                    return "Q";
                case 18:
                    return "R";
                case 19:
                    return "S";
                case 20:
                    return "T";
                case 21:
                    return "U";
                case 22:
                    return "V";
                case 23:
                    return "W";
                case 24:
                    return "X";
                case 25:
                    return "Y";
                case 26:
                    return "Z";
                default:
                    return "";

            }
        }


    }
}

