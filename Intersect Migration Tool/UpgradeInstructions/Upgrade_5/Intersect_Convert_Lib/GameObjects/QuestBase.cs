﻿using System.Collections.Generic;
using System.Linq;
using Intersect.Migration.UpgradeInstructions.Upgrade_5.Intersect_Convert_Lib.GameObjects.Events;

namespace Intersect.Migration.UpgradeInstructions.Upgrade_5.Intersect_Convert_Lib.GameObjects
{
    public enum QuestProgress
    {
        OnAnyTask = 0,
        BeforeTask = 1,
        AfterTask = 2,
        OnTask = 3,
    }

    public struct QuestProgressStruct
    {
        public int Task;
        public int Completed;
        public int TaskProgress;
    }

    public class QuestBase : DatabaseObject
    {
        //General
        public new const string DATABASE_TABLE = "quests";

        public new const GameObject OBJECT_TYPE = GameObject.Quest;
        protected static Dictionary<int, DatabaseObject> sObjects = new Dictionary<int, DatabaseObject>();
        public string BeforeDesc = "";
        public string EndDesc = "";
        public EventBase EndEvent = new EventBase(-1, 0, 0, true);
        public string InProgressDesc = "";
        public byte LogAfterComplete;
        public byte LogBeforeOffer;

        public string Name = "New Quest";

        //Tasks
        public int NextTaskId;

        public byte Quitable;

        public byte Repeatable;

        //Requirements
        //I am cheating here and using event commands as conditional branches instead of having a lot of duplicate code.
        public List<EventCommand> Requirements = new List<EventCommand>();

        public string StartDesc = "";

        //Events
        public EventBase StartEvent = new EventBase(-1, 0, 0, true);

        public List<QuestTask> Tasks = new List<QuestTask>();

        public QuestBase(int id) : base(id)
        {
        }

        public override void Load(byte[] packet)
        {
            var myBuffer = new ByteBuffer();
            myBuffer.WriteBytes(packet);
            Name = myBuffer.ReadString();
            BeforeDesc = myBuffer.ReadString();
            StartDesc = myBuffer.ReadString();
            InProgressDesc = myBuffer.ReadString();
            EndDesc = myBuffer.ReadString();

            Repeatable = myBuffer.ReadByte();
            Quitable = myBuffer.ReadByte();
            LogBeforeOffer = myBuffer.ReadByte();
            LogAfterComplete = myBuffer.ReadByte();

            var requirementCount = myBuffer.ReadInteger();
            Requirements.Clear();
            for (int i = 0; i < requirementCount; i++)
            {
                var cmd = new EventCommand();
                cmd.Load(myBuffer);
                Requirements.Add(cmd);
            }

            NextTaskId = myBuffer.ReadInteger();
            var maxTasks = myBuffer.ReadInteger();
            Tasks.Clear();
            for (int i = 0; i < maxTasks; i++)
            {
                QuestTask task = new QuestTask(myBuffer.ReadInteger())
                {
                    Objective = myBuffer.ReadInteger(),
                    Desc = myBuffer.ReadString(),
                    Data1 = myBuffer.ReadInteger(),
                    Data2 = myBuffer.ReadInteger()
                };
                var taskCompletionEventLength = myBuffer.ReadInteger();
                task.CompletionEvent.Load(myBuffer.ReadBytes(taskCompletionEventLength));

                Tasks.Add(task);
            }

            var startEventLength = myBuffer.ReadInteger();
            StartEvent.Load(myBuffer.ReadBytes(startEventLength));

            var endEventLength = myBuffer.ReadInteger();
            EndEvent.Load(myBuffer.ReadBytes(endEventLength));

            myBuffer.Dispose();
        }

        public byte[] QuestData()
        {
            var myBuffer = new ByteBuffer();
            myBuffer.WriteString(Name);
            myBuffer.WriteString(BeforeDesc);
            myBuffer.WriteString(StartDesc);
            myBuffer.WriteString(InProgressDesc);
            myBuffer.WriteString(EndDesc);

            myBuffer.WriteByte(Repeatable);
            myBuffer.WriteByte(Quitable);
            myBuffer.WriteByte(LogBeforeOffer);
            myBuffer.WriteByte(LogAfterComplete);

            myBuffer.WriteInteger(Requirements.Count);
            for (int i = 0; i < Requirements.Count; i++)
            {
                Requirements[i].Save(myBuffer);
            }

            myBuffer.WriteInteger(NextTaskId);
            myBuffer.WriteInteger(Tasks.Count);
            for (int i = 0; i < Tasks.Count; i++)
            {
                myBuffer.WriteInteger(Tasks[i].Id);
                myBuffer.WriteInteger(Tasks[i].Objective);
                myBuffer.WriteString(Tasks[i].Desc);
                myBuffer.WriteInteger(Tasks[i].Data1);
                myBuffer.WriteInteger(Tasks[i].Data2);

                var taskCompleteionData = Tasks[i].CompletionEvent.GetData();
                myBuffer.WriteInteger(taskCompleteionData.Length);
                myBuffer.WriteBytes(taskCompleteionData);
            }

            var startEventData = StartEvent.GetData();
            myBuffer.WriteInteger(startEventData.Length);
            myBuffer.WriteBytes(startEventData);

            var endEventData = EndEvent.GetData();
            myBuffer.WriteInteger(endEventData.Length);
            myBuffer.WriteBytes(endEventData);

            return myBuffer.ToArray();
        }

        public int GetTaskIndex(int taskId)
        {
            for (int i = 0; i < Tasks.Count; i++)
            {
                if (Tasks[i].Id == taskId) return i;
            }
            return -1;
        }

        public QuestTask FindTask(int taskId)
        {
            for (int i = 0; i < Tasks.Count; i++)
            {
                if (Tasks[i].Id == taskId) return Tasks[i];
            }
            return null;
        }

        public static QuestBase GetQuest(int index)
        {
            if (sObjects.ContainsKey(index))
            {
                return (QuestBase) sObjects[index];
            }
            return null;
        }

        public static string GetName(int index)
        {
            if (sObjects.ContainsKey(index))
            {
                return ((QuestBase) sObjects[index]).Name;
            }
            return "Deleted";
        }

        public override byte[] GetData()
        {
            return QuestData();
        }

        public override string GetTable()
        {
            return DATABASE_TABLE;
        }

        public override GameObject GetGameObjectType()
        {
            return OBJECT_TYPE;
        }

        public static DatabaseObject Get(int index)
        {
            if (sObjects.ContainsKey(index))
            {
                return sObjects[index];
            }
            return null;
        }

        public override void Delete()
        {
            sObjects.Remove(GetId());
        }

        public static void ClearObjects()
        {
            sObjects.Clear();
        }

        public static void AddObject(int index, DatabaseObject obj)
        {
            sObjects.Remove(index);
            sObjects.Add(index, obj);
        }

        public static int ObjectCount()
        {
            return sObjects.Count;
        }

        public static Dictionary<int, QuestBase> GetObjects()
        {
            Dictionary<int, QuestBase> objects = sObjects.ToDictionary(k => k.Key, v => (QuestBase) v.Value);
            return objects;
        }

        public class QuestTask
        {
            public EventBase CompletionEvent = new EventBase(-1, 0, 0, true);
            public int Data1;
            public int Data2;
            public string Desc = "";
            public int Id;
            public int Objective;

            public QuestTask(int id)
            {
                Id = id;
            }

            public string GetTaskString()
            {
                var taskString = "";
                switch (Objective)
                {
                    case 0: //Event Driven
                        taskString = "Event Driven - " + Desc;
                        break;
                    case 1: //Gather Items
                        taskString = "Gather Items [" + ItemBase.GetName(Data1) + " x" + Data2 + "] - " + Desc;
                        break;
                    case 2: //Kill Npcs
                        taskString = "Kill Npc(s) [" + NpcBase.GetName(Data1) + " x" + Data2 + "] - " + Desc;
                        break;
                }
                return taskString;
            }
        }

        public class QuestReward
        {
            public int Amount = 0;
            public int ItemNum = 0;
        }
    }
}