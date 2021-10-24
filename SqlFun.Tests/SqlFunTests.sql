USE [master]
GO
/****** Object:  Database [SqlFunTests]    Script Date: 01.05.2021 22:05:28 ******/
CREATE DATABASE [SqlFunTests]
GO
ALTER DATABASE [SqlFunTests] SET COMPATIBILITY_LEVEL = 150
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [SqlFunTests].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [SqlFunTests] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [SqlFunTests] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [SqlFunTests] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [SqlFunTests] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [SqlFunTests] SET ARITHABORT OFF 
GO
ALTER DATABASE [SqlFunTests] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [SqlFunTests] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [SqlFunTests] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [SqlFunTests] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [SqlFunTests] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [SqlFunTests] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [SqlFunTests] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [SqlFunTests] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [SqlFunTests] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [SqlFunTests] SET  DISABLE_BROKER 
GO
ALTER DATABASE [SqlFunTests] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [SqlFunTests] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [SqlFunTests] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [SqlFunTests] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [SqlFunTests] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [SqlFunTests] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [SqlFunTests] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [SqlFunTests] SET RECOVERY SIMPLE 
GO
ALTER DATABASE [SqlFunTests] SET  MULTI_USER 
GO
ALTER DATABASE [SqlFunTests] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [SqlFunTests] SET DB_CHAINING OFF 
GO
ALTER DATABASE [SqlFunTests] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [SqlFunTests] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [SqlFunTests] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [SqlFunTests] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
ALTER DATABASE [SqlFunTests] SET QUERY_STORE = OFF
GO
USE [SqlFunTests]
GO
/****** Object:  UserDefinedTableType [dbo].[Blog]    Script Date: 01.05.2021 22:05:28 ******/
CREATE TYPE [dbo].[Blog] AS TABLE(
	[id] [int] NOT NULL,
	[name] [nvarchar](50) NOT NULL,
	[title] [nvarchar](250) NOT NULL,
	[description] [nvarchar](max) NOT NULL,
	[owner] [nvarchar](20) NOT NULL,
	[createdAt] [datetime] NOT NULL,
	[modifiedAt] [datetime] NULL,
	[modifiedBy] [nvarchar](20) NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[Tag]    Script Date: 01.05.2021 22:05:28 ******/
CREATE TYPE [dbo].[Tag] AS TABLE(
	[postId] [int] NOT NULL,
	[name] [nvarchar](50) NOT NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[Tag2]    Script Date: 01.05.2021 22:05:28 ******/
CREATE TYPE [dbo].[Tag2] AS TABLE(
	[postId] [int] NULL,
	[name] [nvarchar](50) NOT NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[Tag3]    Script Date: 01.05.2021 22:05:28 ******/
CREATE TYPE [dbo].[Tag3] AS TABLE(
	[postId] [int] NOT NULL,
	[name] [nvarchar](50) NOT NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[Tag4]    Script Date: 01.05.2021 22:05:28 ******/
CREATE TYPE [dbo].[Tag4] AS TABLE(
	[postId] [int] NOT NULL,
	[name] [nvarchar](50) NOT NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[Tag5]    Script Date: 01.05.2021 22:05:28 ******/
CREATE TYPE [dbo].[Tag5] AS TABLE(
	[postId] [int] NOT NULL,
	[name] [nvarchar](50) NOT NULL,
	[status] [char](1) NOT NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[Tag6]    Script Date: 01.05.2021 22:05:28 ******/
CREATE TYPE [dbo].[Tag6] AS TABLE(
	[postId] [int] NOT NULL,
	[name] [nvarchar](50) NOT NULL,
	[status] [int] NOT NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[UserProfile]    Script Date: 01.05.2021 22:05:28 ******/
CREATE TYPE [dbo].[UserProfile] AS TABLE(
	[id] [nvarchar](20) NOT NULL,
	[name] [nvarchar](80) NOT NULL,
	[email] [nvarchar](200) NOT NULL,
	[avatar] [varbinary](max) NOT NULL
)
GO
/****** Object:  Table [dbo].[Blog]    Script Date: 01.05.2021 22:05:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Blog](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[name] [nvarchar](50) NOT NULL,
	[title] [nvarchar](250) NOT NULL,
	[description] [nvarchar](max) NOT NULL,
	[owner] [nvarchar](20) NOT NULL,
	[createdAt] [datetime] NOT NULL,
	[modifiedAt] [datetime] NULL,
	[modifiedBy] [nvarchar](20) NULL,
 CONSTRAINT [PK_Blog_1] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Comment]    Script Date: 01.05.2021 22:05:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Comment](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[postId] [int] NOT NULL,
	[parentId] [int] NULL,
	[content] [nvarchar](max) NOT NULL,
	[author] [nvarchar](20) NOT NULL,
	[createdAt] [datetime] NULL,
 CONSTRAINT [PK_Comment] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Post]    Script Date: 01.05.2021 22:05:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Post](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[blogId] [int] NOT NULL,
	[name] [nvarchar](50) NOT NULL,
	[title] [nvarchar](250) NOT NULL,
	[content] [nvarchar](max) NOT NULL,
	[author] [nvarchar](20) NOT NULL,
	[createdAt] [datetime] NOT NULL,
	[modifiedAt] [datetime] NULL,
	[modifiedBy] [nvarchar](20) NULL,
	[status] [char](1) NOT NULL,
 CONSTRAINT [PK_Post] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[post2]    Script Date: 01.05.2021 22:05:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[post2](
	[id] [int] NOT NULL,
	[blogId] [int] NOT NULL,
	[name] [nvarchar](50) NOT NULL,
	[title] [nvarchar](250) NOT NULL,
	[content] [nvarchar](max) NOT NULL,
	[author] [nvarchar](20) NOT NULL,
	[createdAt] [datetime] NOT NULL,
	[modifiedAt] [datetime] NULL,
	[modifiedBy] [nvarchar](20) NULL,
	[status] [char](1) NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Tag]    Script Date: 01.05.2021 22:05:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tag](
	[postId] [int] NOT NULL,
	[name] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_Tag] PRIMARY KEY CLUSTERED 
(
	[postId] ASC,
	[name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserProfile]    Script Date: 01.05.2021 22:05:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserProfile](
	[id] [nvarchar](20) NOT NULL,
	[name] [nvarchar](80) NOT NULL,
	[email] [nvarchar](200) NOT NULL,
	[avatar] [varbinary](max) NOT NULL,
 CONSTRAINT [PK_UserProfile] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[Blog] ON 

INSERT [dbo].[Blog] ([id], [name], [title], [description], [owner], [createdAt], [modifiedAt], [modifiedBy]) VALUES (1, N'functional-data-access-with-sqlfun', N'Functional data access with SqlFun', N'Designing functional-relational mapper with F#', N'jacentino', CAST(N'2017-05-28T21:35:00.000' AS DateTime), NULL, NULL)
SET IDENTITY_INSERT [dbo].[Blog] OFF
GO
SET IDENTITY_INSERT [dbo].[Comment] ON 

INSERT [dbo].[Comment] ([id], [postId], [parentId], [content], [author], [createdAt]) VALUES (1, 1, NULL, N'Great, informative article!', N'joeblack', CAST(N'2017-05-30T16:47:00.000' AS DateTime))
INSERT [dbo].[Comment] ([id], [postId], [parentId], [content], [author], [createdAt]) VALUES (2, 1, 1, N'Thank you!', N'jacenty', CAST(N'2017-06-01T12:35:00.000' AS DateTime))
INSERT [dbo].[Comment] ([id], [postId], [parentId], [content], [author], [createdAt]) VALUES (3, 1, 2, N'You''re welcome!', N'joeblack', CAST(N'2017-06-02T17:44:00.000' AS DateTime))
SET IDENTITY_INSERT [dbo].[Comment] OFF
GO
SET IDENTITY_INSERT [dbo].[Post] ON 

INSERT [dbo].[Post] ([id], [blogId], [name], [title], [content], [author], [createdAt], [modifiedAt], [modifiedBy], [status]) VALUES (1, 1, N'another-sql-framework', N'Yet another sql framework', N'There are so many solutions for this problem. What is the case for another one?', N'jacenty', CAST(N'2017-05-29T22:15:00.000' AS DateTime), NULL, NULL, N'P')
INSERT [dbo].[Post] ([id], [blogId], [name], [title], [content], [author], [createdAt], [modifiedAt], [modifiedBy], [status]) VALUES (2, 1, N'whats-wrong-with-existing-f', N'What''s wrong with existing frameworks', N'Shortly - they not align with functional paradigm.', N'jacenty', CAST(N'2017-06-06T19:00:00.000' AS DateTime), NULL, NULL, N'P')
SET IDENTITY_INSERT [dbo].[Post] OFF
GO
INSERT [dbo].[Tag] ([postId], [name]) VALUES (1, N'existing')
INSERT [dbo].[Tag] ([postId], [name]) VALUES (1, N'framework')
INSERT [dbo].[Tag] ([postId], [name]) VALUES (1, N'options')
GO
INSERT [dbo].[UserProfile] ([id], [name], [email], [avatar]) VALUES (N'jacenty', N'Jacek Placek', N'jacek.placek@pp.com', NULL)
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Blog_1]    Script Date: 01.05.2021 22:05:29 ******/
ALTER TABLE [dbo].[Blog] ADD  CONSTRAINT [IX_Blog_1] UNIQUE NONCLUSTERED 
(
	[name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Post]    Script Date: 01.05.2021 22:05:29 ******/
ALTER TABLE [dbo].[Post] ADD  CONSTRAINT [IX_Post] UNIQUE NONCLUSTERED 
(
	[blogId] ASC,
	[name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Comment]  WITH CHECK ADD  CONSTRAINT [FK_Comment_Comment] FOREIGN KEY([parentId])
REFERENCES [dbo].[Comment] ([id])
GO
ALTER TABLE [dbo].[Comment] CHECK CONSTRAINT [FK_Comment_Comment]
GO
ALTER TABLE [dbo].[Comment]  WITH CHECK ADD  CONSTRAINT [FK_Comment_Post] FOREIGN KEY([postId])
REFERENCES [dbo].[Post] ([id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Comment] CHECK CONSTRAINT [FK_Comment_Post]
GO
ALTER TABLE [dbo].[Post]  WITH CHECK ADD  CONSTRAINT [FK_Post_Blog] FOREIGN KEY([blogId])
REFERENCES [dbo].[Blog] ([id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Post] CHECK CONSTRAINT [FK_Post_Blog]
GO
ALTER TABLE [dbo].[Tag]  WITH CHECK ADD  CONSTRAINT [FK_Tag_Post] FOREIGN KEY([postId])
REFERENCES [dbo].[Post] ([id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Tag] CHECK CONSTRAINT [FK_Tag_Post]
GO
/****** Object:  StoredProcedure [dbo].[FindPosts]    Script Date: 01.05.2021 22:05:29 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
 
CREATE PROCEDURE [dbo].[FindPosts]
	@blogId int,
	@title nvarchar(200),
	@content nvarchar(max),
	@author nvarchar(50),
	@createdAtFrom datetime,
	@createdAtTo datetime,
	@modifiedAtFrom datetime,
	@modifiedAtTo datetime,
	@status char(1)
AS
BEGIN
	SET NOCOUNT ON;

select id, blogId, name, title, content, author, createdAt, modifiedAt, modifiedBy, status from post
where (blogId = @blogId or @blogId is null)
	and (title like '%' + @title + '%' or @title is null)
	and (content like '%' + @content + '%' or @content is null)
	and (author = @author or @author is null)
	and (createdAt >= @createdAtFrom or @createdAtFrom is null)
	and (createdAt <= @createdAtTo or @createdAtTo is null)
	and (modifiedAt >= @modifiedAtFrom or @modifiedAtFrom is null)
	and (modifiedAt <= @modifiedAtTo or @modifiedAtTo is null)
	and (status = @status or @status is null)
END

GO
/****** Object:  StoredProcedure [dbo].[GetAllPosts]    Script Date: 01.05.2021 22:05:29 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[GetAllPosts]
	@blogId int
AS
BEGIN
	SET NOCOUNT ON;

	select id, blogId, name, title, content, author, createdAt, modifiedAt, modifiedBy, status 
	from post 
	where blogId = @blogId;
    
	select c.id, c.postId, c.parentId, c.content, c.author, c.createdAt 
	from comment c join post p on c.postId = p.id 
	where p.blogId = @blogId
    
	select t.postId, t.name 
	from tag t join post p on t.postId = p.id 
	where p.blogId = @blogId;
END

GO
USE [master]
GO
ALTER DATABASE [SqlFunTests] SET  READ_WRITE 
GO
