/****************************************************************************
 * Copyright (c) 2017 liangxieq
 * 
 * https://github.com/UniPM/UniPM
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 * 
 * http://liangxiegame.com
 * https://github.com/liangxiegame/QFramework
 * https://github.com/liangxiegame/QSingleton
 * https://github.com/liangxiegame/QChain
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 ****************************************************************************/

using QFramework.Libs;

namespace UniPM
{
	using UnityEditor;
	using UnityEngine;
	using System.IO;
	using UniRx;
	using QFramework;

	/// <summary>
	///  TODO: 交互优化:
	/// 	1.提供右键就可以支持上传。
	/// 	2.要有个显示列表支持。
	///  TODO:功能:
	/// 	1.要提供一个全局的配置。
	/// </summary>
	public class UniPMWindow : QEditorWindow
	{
		[MenuItem("UniPM/Open")]
		static void Open()
		{


			PackageManagerConfig.GetRemote(config =>
			{
				UniPMWindow frameworkConfigEditorWindow = (UniPMWindow) GetWindow(typeof(UniPMWindow), true);
				frameworkConfigEditorWindow.titleContent = new GUIContent("QFrameworkConfig");
				frameworkConfigEditorWindow.position = new Rect(Screen.width / 2, Screen.height / 2, 800, 600f);
				frameworkConfigEditorWindow.LocalConfig = config;
				frameworkConfigEditorWindow.Init();
				frameworkConfigEditorWindow.Show();
			});
		}

		[MenuItem("Assets/UniPM/MakePackage")]
		static void MakePackage()
		{
			string packagePath = MouseSelector.GetSelectedPathOrFallback();
			string packageConfigPath = Path.Combine(packagePath, "Package.json");

			PackageConfig packageConfig = null;
			if (File.Exists(packageConfigPath))
			{
				packageConfig = PackageConfig.LoadFromPath(packageConfigPath);
				packageConfig.PackagePath = packagePath;
			}
			else
			{
				packageConfig = new PackageConfig(packagePath);
			}
			packageConfig.SaveLocal();
		}

		[MenuItem("Assets/UniPM/UploadPackage")]
		static void UploadPackage()
		{
			string packagePath = MouseSelector.GetSelectedPathOrFallback();
			string packageConfigPath = Path.Combine(packagePath, "Package.json");
			
			var packageConfig = new PackageConfig(packagePath);
			packageConfig.SaveLocal();
		}
		
		[MenuItem("Assets/UniPM/Version/Update (x.0.0")]
		static void UpdateMajorVersion()
		{
			string packagePath = MouseSelector.GetSelectedPathOrFallback();
			string packageConfigPath = Path.Combine(packagePath, "Package.json");
			if (File.Exists(packageConfigPath))
			{
				PackageConfig config = PackageConfig.LoadFromPath(packageConfigPath);
				config.UpdateVersion(0);
				config.SaveLocal();
			}
		}
		
		[MenuItem("Assets/UniPM/Version/Update (0.x.0")]
		static void UpdateMiddleVersion()
		{
			string packagePath = MouseSelector.GetSelectedPathOrFallback();
			string packageConfigPath = Path.Combine(packagePath, "Package.json");
			if (File.Exists(packageConfigPath))
			{
				PackageConfig config = PackageConfig.LoadFromPath(packageConfigPath);
				config.UpdateVersion(1);
				config.SaveLocal();
			}
		}
		
		[MenuItem("Assets/UniPM/Version/Update (0.0.x")]
		static void UpdateSubVersion()
		{
			string packagePath = MouseSelector.GetSelectedPathOrFallback();
			string packageConfigPath = Path.Combine(packagePath, "Package.json");
			if (File.Exists(packageConfigPath))
			{
				PackageConfig config = PackageConfig.LoadFromPath(packageConfigPath);
				config.UpdateVersion(2);
				config.SaveLocal();
			}
			else
			{
				Log.W("no package.json file in folder:{0}",packagePath);
			}
		}

		[MenuItem("Assets/UniPM/Server/CopyToServer")]
		static void CopyToServer()
		{
			string packagePath = MouseSelector.GetSelectedPathOrFallback();
			string packageConfigPath = Path.Combine(packagePath, "Package.json");
			if (File.Exists(packageConfigPath))
			{
				string err = string.Empty;

				PackageConfig config = PackageConfig.LoadFromPath(packageConfigPath);
				string serverUploaderPath = Application.dataPath.CombinePath("QUGame/QGamePluginServer");

				IOUtils.DeleteDirIfExists(serverUploaderPath.CombinePath(config.Name));
				ZipUtil.ZipDirectory(config.PackagePath,
					IOUtils.CreateDirIfNotExists(serverUploaderPath.CombinePath(config.Name)).CombinePath(config.Name + ".zip"));

				string toConfigFilePath = serverUploaderPath.CombinePath(config.Name).CombinePath("Package.json");

				IOUtils.DeleteFileIfExists(toConfigFilePath);
				
				File.Copy(config.ConfigFilePath,toConfigFilePath);
				
				
				PackageManagerConfig.GetLocal().SaveExport();
				AssetDatabase.Refresh();
			}
			else
			{
				Log.W("no package.json file in folder:{0}",packagePath);
			}
		}
		
		/// <summary>
		/// 现在成功的PackageListPage
		/// </summary>
		private InstalledPackageListPage mInstalledPackageListPage;
		
		void Init()
		{
			mInstalledPackageListPage = new InstalledPackageListPage(LocalConfig);
			AddChild(mInstalledPackageListPage);
		}

		public PackageManagerConfig LocalConfig;

		public static string ServerURL = "http://code.putao.io/liqingyun/QGamePluginServer/";

		public static void DownloadZip(PackageConfig config)
		{
			ObservableWWW
				.GetAndGetBytes(config.DownloadURL).Subscribe(
					bytes =>
					{
						string tempZipFile = Application.persistentDataPath + "/temp.zip";
						File.WriteAllBytes(tempZipFile, bytes);
						string err = string.Empty;
						IOUtils.DeleteDirIfExists(config.PackagePath);
						ZipUtil.UnZipFile(tempZipFile, config.PackagePath);
						File.Delete(tempZipFile);

						AssetDatabase.Refresh();
						config.SaveLocal();
					});
		}

		[MenuItem("UniPM/ExportCompress")]
		public static void Export()
		{
			string err = string.Empty;

//			var packageData = new PackageConfig();
//			ZipUtil.ZipFile(packageData.FolderFullPath, packageData.ZipFileFullPath, ".json", out err);
//			packageData.SaveExport();
			AssetDatabase.Refresh();

			err.ToLog();
		}

		[MenuItem("UniPM/Test")]
		public static void Test()
		{
			// "http://code.putao.io/liqingyun/QGamePluginServer/raw/master/PackageList.json"
			ObservableWWW.Get("http://www.baidu.com")
				.Subscribe(
					jsonContent =>
					{
						if (string.IsNullOrEmpty(jsonContent)) Log.E("is null or empty");
						
					}, err =>
					{
						Log.E(err);
					});
			
			


//			var configFileList = IOUtils.GetDirSubFilePathList(new PackageConfig().FolderFullPath, true, "Config.json");
//			configFileList.ForEach(fileName => fileName.Log());
		}

		[MenuItem("UniPM/ExtractServer")]
		public static void ExtractGameServer()
		{
			PackageManagerConfig.GetLocal().SaveExport();
		}

		[MenuItem("UniPM/UpdateCompress")]
		public static void UpdateCompress()
		{
			ObservableWWW
				.GetAndGetBytes(ServerURL + "raw/master/TestCompress/TestCompress.zip").Subscribe(
					bytes =>
					{
						string tempZipFile = Application.persistentDataPath + "/temp.zip";
						File.WriteAllBytes(tempZipFile, bytes);
						string err = string.Empty;
//						IOUtils.DeleteDirIfExists(new PackageConfig().FolderFullPath);
//						ZipUtil.UnZipFile(tempZipFile, new PackageConfig().FolderFullPath, out err);
						File.Delete(tempZipFile);

//						new PackageConfig().SaveLocal();

						AssetDatabase.Refresh();
					});
		}
	}
}