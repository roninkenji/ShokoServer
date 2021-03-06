﻿using System;
using System.Xml;
using Shoko.Commons.Queue;
using Shoko.Models.Azure;
using Shoko.Models.Enums;
using Shoko.Models.Queue;
using Shoko.Models.Server;
using Shoko.Server.Providers.Azure;

namespace Shoko.Server.Commands
{
    public class CommandRequest_WebCacheDeleteXRefAniDBOther : CommandRequestImplementation, ICommandRequest
    {
        public int AnimeID { get; set; }
        public int CrossRefType { get; set; }

        public CommandRequestPriority DefaultPriority
        {
            get { return CommandRequestPriority.Priority9; }
        }

        public QueueStateStruct PrettyDescription
        {
            get
            {
                return new QueueStateStruct()
                {
                    queueState = QueueStateEnum.WebCacheDeleteXRefAniDBOther,
                    extraParams = new string[] {AnimeID.ToString()}
                };
            }
        }

        public CommandRequest_WebCacheDeleteXRefAniDBOther()
        {
        }

        public CommandRequest_WebCacheDeleteXRefAniDBOther(int animeID, CrossRefType xrefType)
        {
            this.AnimeID = animeID;
            this.CommandType = (int) CommandRequestType.WebCache_DeleteXRefAniDBOther;
            this.CrossRefType = (int) xrefType;
            this.Priority = (int) DefaultPriority;

            GenerateCommandID();
        }

        public override void ProcessCommand()
        {
            try
            {
                AzureWebAPI.Delete_CrossRefAniDBOther(AnimeID, (CrossRefType) CrossRefType);
            }
            catch (Exception ex)
            {
                logger.Error(ex, 
                    "Error processing CommandRequest_WebCacheDeleteXRefAniDBOther: {0}" + ex.ToString());
                return;
            }
        }

        public override void GenerateCommandID()
        {
            this.CommandID = string.Format("CommandRequest_WebCacheDeleteXRefAniDBOther_{0}_{1}", AnimeID,
                CrossRefType);
        }

        public override bool LoadFromDBCommand(CommandRequest cq)
        {
            this.CommandID = cq.CommandID;
            this.CommandRequestID = cq.CommandRequestID;
            this.CommandType = cq.CommandType;
            this.Priority = cq.Priority;
            this.CommandDetails = cq.CommandDetails;
            this.DateTimeUpdated = cq.DateTimeUpdated;

            // read xml to get parameters
            if (this.CommandDetails.Trim().Length > 0)
            {
                XmlDocument docCreator = new XmlDocument();
                docCreator.LoadXml(this.CommandDetails);

                // populate the fields
                this.AnimeID =
                    int.Parse(TryGetProperty(docCreator, "CommandRequest_WebCacheDeleteXRefAniDBOther", "AnimeID"));
                this.CrossRefType =
                    int.Parse(TryGetProperty(docCreator, "CommandRequest_WebCacheDeleteXRefAniDBOther",
                        "CrossRefType"));
            }

            return true;
        }

        public override CommandRequest ToDatabaseObject()
        {
            GenerateCommandID();

            CommandRequest cq = new CommandRequest
            {
                CommandID = this.CommandID,
                CommandType = this.CommandType,
                Priority = this.Priority,
                CommandDetails = this.ToXML(),
                DateTimeUpdated = DateTime.Now
            };
            return cq;
        }
    }
}