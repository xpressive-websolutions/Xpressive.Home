
create table Script (
    Id uniqueidentifier not null primary key,
    Name nvarchar(64) not null,
    JavaScript nvarchar(max) not null,
    IsEnabled bit not null
)

create table ScheduledScript (
    Id uniqueidentifier not null primary key,
    ScriptId uniqueidentifier not null,
    CronTab nvarchar(32) not null,

    constraint FK_ScheduledScript_Script foreign key (ScriptId) references Script (Id)
)

create table TriggeredScript (
    Id uniqueidentifier not null primary key,
    ScriptId uniqueidentifier not null,
    Variable nvarchar(255) not null,

    constraint FK_TriggeredScript_Script foreign key (ScriptId) references Script (Id)
)

create table Variable (
    Name nvarchar(255) not null primary key,
    DataType nvarchar(16) not null,
    Value nvarchar(max) not null
)

create table Device (
    Gateway nvarchar(16) not null,
    Id nvarchar(64) not null primary key,
    Name nvarchar(64) not null,
    Properties nvarchar(max) not null
)

create table Room (
    Id uniqueidentifier not null primary key,
    Name nvarchar(64) not null,
    Icon nvarchar(512) not null,
    SortOrder int not null
)

create table RoomScriptGroup (
    Id uniqueidentifier not null primary key,
    RoomId uniqueidentifier not null,
    Name nvarchar(64) not null,
    Icon nvarchar(512) not null,
    SortOrder int not null,

    constraint FK_RoomScriptGroup_Room foreign key (RoomId) references Room (Id)
)

create table RoomScript (
    Id uniqueidentifier not null primary key,
    GroupId uniqueidentifier not null,
    ScriptId uniqueidentifier not null,
    Name nvarchar(64) not null,

    constraint FK_RoomScript_RoomScriptGroup foreign key (GroupId) references RoomScriptGroup (Id),
    constraint FK_RoomScript_Script foreign key (ScriptId) references Script (Id)
)
