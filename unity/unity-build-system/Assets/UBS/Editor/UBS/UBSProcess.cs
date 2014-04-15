// ------------------------------------------------------------------------------
//  <autogenerated>
//      This code was generated by a tool.
//      Mono Runtime Version: 4.0.30319.1
// 
//      Changes to this file may cause incorrect behavior and will be lost if 
//      the code is regenerated.
//  </autogenerated>
// ------------------------------------------------------------------------------
using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace UBS
{
	[Serializable]
	public class UBSProcess : ScriptableObject
	{
		const string kProcessPath = "Assets/UBSProcess.asset";
		const string kProcessPathKey = "UBSProcessPath";



		#region data

		public static UBSBuildBehavior BuildBehavior 
		{
			get 
			{
				return UnityEditorInternal.InternalEditorUtility.HasPro () ? 
					UBSBuildBehavior.auto : UBSBuildBehavior.manual;
			}
		}

		[SerializeField]
		BuildConfiguration mCurrentBuildConfiguration;
		BuildConfiguration CurrentBuildConfiguration
		{
			get {return mCurrentBuildConfiguration ;}

		}

		[SerializeField]
		bool mBuildAndRun;

		[SerializeField]
		BuildCollection mCollection;
		public BuildCollection BuildCollection
		{
			get { return mCollection; }
		}

		[SerializeField]
		List<BuildProcess>mSelectedProcesses;


		[SerializeField]
		int mCurrentBuildProcessIndex;

		[SerializeField]
		int mCurrent;

		[SerializeField]
		UBSState mCurrentState = UBSState.invalid;

		[SerializeField]
		UBSStepListWalker mPreStepWalker = new UBSStepListWalker();

		[SerializeField]
		UBSStepListWalker mPostStepWalker = new UBSStepListWalker();

		public UBSState CurrentState
		{
			get { return mCurrentState; }
		}

		public UBSStepListWalker SubPreWalker
		{
			get {
				return mPreStepWalker;
			}
		}

		public UBSStepListWalker SubPostWalker
		{
			get {
				return mPostStepWalker;
			}
		}

		public float Progress
		{
			get {

				return ((SubPreWalker.Progress + SubPostWalker.Progress) / 2.0f
					+ System.Math.Max(0, mCurrentBuildProcessIndex-1 )) / (float)mSelectedProcesses.Count;
			}
		}

		public string CurrentProcessName
		{
			get
			{
				if(CurrentProcess != null)
					return CurrentProcess.mName;
				return "N/A";
			}
		}

		BuildProcess CurrentProcess
		{
			get
			{
				if(mSelectedProcesses == null || mCurrentBuildProcessIndex >= mSelectedProcesses.Count)
					return null;
				return mSelectedProcesses[mCurrentBuildProcessIndex];
			}
		}

		#endregion

		#region public interface

		public BuildProcess GetCurrentProcess()
		{
			return CurrentProcess;
		}

		public static string GetProcessPath()
		{
			return EditorPrefs.GetString(kProcessPathKey, kProcessPath);
		}
		/// <summary>
		/// You can overwrite where to store the build process. 
		/// </summary>
		/// <param name="pPath">P path.</param>
		public static void SetProcessPath(string pPath)
		{
			EditorPrefs.GetString(kProcessPathKey, kProcessPath);
		}


		public static void Create(BuildCollection pCollection, bool pBuildAndRun)
		{
			UBSProcess p = ScriptableObject.CreateInstance<UBSProcess>();
			p.mBuildAndRun = pBuildAndRun;
			p.mCollection = pCollection;
			p.mSelectedProcesses = p.mCollection.mProcesses.FindAll( obj => obj.mSelected );
			p.mCurrentState = UBSState.invalid;


			AssetDatabase.CreateAsset( p, GetProcessPath());
			AssetDatabase.SaveAssets();



		}
		public static bool IsUBSProcessRunning()
		{
			var asset = AssetDatabase.LoadAssetAtPath( GetProcessPath(), typeof(UBSProcess) );
			return asset != null;
		}
		public static UBSProcess LoadUBSProcess()
		{
			var process = AssetDatabase.LoadAssetAtPath( GetProcessPath(), typeof(UBSProcess));
			return process as UBSProcess;
		}
		

		public void MoveNext()
		{

			switch(CurrentState)
			{
				case UBSState.setup: DoSetup(); break;
				case UBSState.preSteps: DoPreSteps(); break;
				case UBSState.building: DoBuilding(); break;
				case UBSState.postSteps: DoPostSteps(); break;
				case UBSState.invalid: NextBuild(); break;
				case UBSState.done: OnDone(); break;
			}
		}
		
		public void Cancel(string pMessage)
		{
			if(pMessage.Length > 0)
			{
				EditorUtility.DisplayDialog("UBS: Error occured!", pMessage, "Ok - my fault.");
			}
			Cancel();
		}

		public void Cancel()
		{
			mCurrentState = UBSState.aborted;
			mPreStepWalker.Clear();
			mPostStepWalker.Clear();
			Save();
		}

		#endregion


		#region build process state handling
		void OnDone()
		{

		}
		void NextBuild()
		{

			if(mCurrentBuildProcessIndex >= mSelectedProcesses.Count)
			{
				mCurrentState = UBSState.done;
				Save();
			}else
			{
				mCurrentState = UBSState.setup;
				Save ();
			}
		}

		void DoSetup()
		{
			mCurrentBuildConfiguration = new BuildConfiguration();
			if(!CheckOutputPath(CurrentProcess))
				return;

			EditorUserBuildSettings.SwitchActiveBuildTarget(CurrentProcess.mPlatform);
			
			var scenes = new EditorBuildSettingsScene[CurrentProcess.mScenes.Count];
			for(int i = 0;i< scenes.Length;i++)
			{
				EditorBuildSettingsScene ebss = new EditorBuildSettingsScene( CurrentProcess.mScenes[i] ,true );
				scenes[i] = ebss;
			}
			EditorBuildSettings.scenes = scenes;


			mPreStepWalker.Init( CurrentProcess.mPreBuildSteps, mCurrentBuildConfiguration );

			mPostStepWalker.Init(CurrentProcess.mPostBuildSteps, mCurrentBuildConfiguration );

			mCurrentState = UBSState.preSteps;
			
			Save();
		}

		void DoPreSteps()
		{
			mPreStepWalker.MoveNext();
			if(mPreStepWalker.IsDone())
			{
				mCurrentState = UBSState.building;
			}
			Save();
		}

		void DoBuilding()
		{

			if(BuildPipeline.isBuildingPlayer || BuildBehavior != UBSBuildBehavior.auto)
				return;

			if (BuildBehavior != UBSBuildBehavior.auto) 
			{
				/*
				System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(EditorWindow));
				var M = asm
					.GetType("UnityEditor.BuildPlayerWindow")
					.GetMethod("ShowBuildPlayerWindow", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
				M.Invoke(null, null);
				*/
				return;
			}

			List<string> scenes = new List<string>();

			foreach(var scn in EditorBuildSettings.scenes)
			{
				if(scn.enabled)
					scenes.Add(scn.path);
			}
			BuildOptions bo = CurrentProcess.mBuildOptions;
			if(mBuildAndRun)
				bo = bo | BuildOptions.AutoRunPlayer;

			BuildPipeline.BuildPlayer(
				scenes.ToArray(),
				CurrentProcess.mOutputPath,
				CurrentProcess.mPlatform,
				bo );


			OnBuildDone ();
		}

		void OnBuildDone() 
		{
			mCurrentState = UBSState.postSteps;
			Save();
		}

		[UnityEditor.Callbacks.PostProcessBuild]
		public static void OnPostProcessBuild(BuildTarget target, string buildPath)
		{
			if (BuildBehavior == UBSBuildBehavior.auto)
				return;

			buildPath = UBS.Helpers.GetProjectRelativePath (buildPath);
			UBSProcess p = UBSProcess.LoadUBSProcess ();
			if (p.mCurrentState == UBSState.building && target == p.CurrentProcess.mPlatform)
			{
				if (p.CurrentProcess.mOutputPath != buildPath) 
				{
					Debug.Log(
						string.Format("Manually selected build path \"{0}\" differs from specified UBS build path \"{1}\" in process \"{2}\". Using manually selected one.",
					    	buildPath, p.CurrentProcess.mOutputPath, p.CurrentProcessName)
						);
					p.CurrentProcess.mOutputPath = buildPath;
				}
				p.OnBuildDone();
			}
		}

		void DoPostSteps()
		{
			mPostStepWalker.MoveNext();

			if(mPostStepWalker.IsDone())
			{
				mCurrentState = UBSState.invalid;
				mCurrentBuildProcessIndex++;
			}
			Save();
		}
		#endregion

		void Save()
		{
			if(this != null) {
				EditorUtility.SetDirty(this);
				AssetDatabase.SaveAssets();
			}
		}

		bool CheckOutputPath(BuildProcess pProcess)
		{
			string error = "";
			
			
			if(pProcess.mOutputPath.Length == 0) {
				error = "Please provide an output path.";
				Cancel(error);
				return false;
			}
			
			try
			{
				DirectoryInfo dir;
				if(pProcess.mPlatform == BuildTarget.Android)
					dir = new DirectoryInfo(Path.GetDirectoryName(pProcess.mOutputPath));
				else
					dir = new DirectoryInfo(pProcess.mOutputPath);
				
				if(!dir.Exists)
					error = "The given output path is invalid.";
			}
			catch (Exception e)
			{
				error = e.ToString();
			}
			
			if(error.Length > 0)
			{
				Cancel(error);
				return false;
			}
			return true;

		}
	}

	public enum UBSBuildBehavior {
		auto,
		manual
	}

	public enum UBSState
	{
		invalid,
		setup,
		preSteps,
		building,
		postSteps,
		done,
		aborted
	}



	[Serializable]
	public class UBSStepListWalker
	{
		[SerializeField]
		int mIndex = 0;
		[SerializeField]
		List<BuildStep> mSteps;


		IBuildStepProvider mCurrentStep;
		
		[SerializeField]
		BuildConfiguration mConfiguration;
		
		public UBSStepListWalker()
		{

		}

		public void Init ( List<BuildStep> pSteps, BuildConfiguration pConfiguration)
		{
			mIndex = 0;
			mSteps = pSteps;
			mConfiguration = pConfiguration;
		}
		public void Clear()
		{
			mIndex = 0;
			mSteps = null;
			mConfiguration = null;
		}

		public void MoveNext()
		{
			if(mCurrentStep == null || mCurrentStep.IsBuildStepDone())
			{
				NextStep();
			}else
			{
				mCurrentStep.BuildStepUpdate();
			}
		}
		
		void NextStep()
		{
			if(mSteps == null)
				return;
			if(IsDone())
			{
				return;
			}

			if(mCurrentStep != null)
				mIndex++;

			if(mIndex >= mSteps.Count)
				return;

			mSteps[mIndex].InferType();
			
			mCurrentStep = System.Activator.CreateInstance( mSteps[mIndex].mType ) as IBuildStepProvider;
			mConfiguration.SetParams( mSteps[mIndex].mParams );
			mCurrentStep.BuildStepStart(mConfiguration);
			
		}

		public bool IsDone()
		{
			if(mSteps != null)
				return mIndex == mSteps.Count;
			else
				return false;
		}

		public float Progress
		{
			get
			{
				if(mSteps == null || mSteps.Count == 0)
					return 0;


				return mIndex / (float) mSteps.Count;
			}
		}
		public string Step
		{
			get
			{
				if(mSteps == null || mIndex >= mSteps.Count)
					return "N/A";

				return mSteps[mIndex].mTypeName;
			}
		}
	}



}

