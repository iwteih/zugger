using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.IsolatedStorage;
using System.IO;
using System.Runtime.Serialization;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using log4net;

namespace ZuggerWpf
{
    public class IOHelper
    {
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region IsolatedStorageFile

        private static ApplicationConfig appConfig;

        public static ApplicationConfig LoadIsolatedData()
        {
            if (appConfig == null)
            {
                byte[] byteContent = null;

                using (IsolatedStorageFile f = IsolatedStorageFile.GetUserStoreForAssembly())
                {
                    using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream("Zugger", FileMode.OpenOrCreate, f))
                    {
                        byteContent = new byte[stream.Length];

                        stream.Read(byteContent, 0, byteContent.Length);
                    }

                    appConfig = DeserializeFromByteArray<ApplicationConfig>(byteContent);
                }

                return appConfig == null ? new ApplicationConfig() : appConfig;
            }

            return appConfig;
        }

        public static void SaveIsolatedData(ApplicationConfig config)
        {
            byte[] byteContent = SerializeToByteArray<ApplicationConfig>(config);

            if (byteContent == null)
            {
                return;
            }

            using (IsolatedStorageFile f = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream("Zugger", FileMode.Create, f))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.Write(byteContent, 0, byteContent.Length);
                }
            }
        }
        #endregion

        public static byte[] SerializeToByteArray<T>(T obj) where T : class
        {
            if (obj == null)
            {
                return null;
            }

            byte[] byteContent = null;

            IFormatter formatter = new BinaryFormatter();

            using (MemoryStream ms = new MemoryStream())
            {
                try
                {
                    formatter.Serialize(ms, obj);
                }
                catch (Exception exp)
                {
                    logger.Error(string.Format("SerializeToByteArray error: {0}", exp.Message));
                }

                byteContent = ms.GetBuffer();

                byteContent = ms.ToArray();
            }

            return byteContent;
        }

        public static T DeserializeFromByteArray<T>(byte[] byteArray) where T : class
        {
            if (byteArray == null
                || byteArray.Length == 0)
            {
                return default(T);
            }

            object obj = null;

            IFormatter formatter = new BinaryFormatter();
    
            using (MemoryStream ms = new MemoryStream(byteArray))
            {
                try
                {
                    obj = formatter.Deserialize(ms);
                }
                catch (Exception exp)
                {
                    logger.Error(string.Format("Deserialize error: {0}", exp.Message));
                }
            }

            T target = obj as T;

            return target;
        }
    }
}
