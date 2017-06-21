USE [master]
GO
/****** Object:  Database [SqlFunTests]    Script Date: 18.06.2017 13:51:50 ******/
CREATE DATABASE [SqlFunTests]
 
GO
ALTER DATABASE [SqlFunTests] SET COMPATIBILITY_LEVEL = 110
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
ALTER DATABASE [SqlFunTests] SET AUTO_CREATE_STATISTICS ON 
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
ALTER DATABASE [SqlFunTests] SET TARGET_RECOVERY_TIME = 0 SECONDS 
GO
USE [SqlFunTests]
GO
/****** Object:  UserDefinedTableType [dbo].[Tag]    Script Date: 18.06.2017 13:51:50 ******/
CREATE TYPE [dbo].[Tag] AS TABLE(
	[postId] [int] NOT NULL,
	[name] [nvarchar](50) NOT NULL
)
GO
/****** Object:  StoredProcedure [dbo].[FindPosts]    Script Date: 18.06.2017 13:51:50 ******/
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
/****** Object:  StoredProcedure [dbo].[GetAllPosts]    Script Date: 18.06.2017 13:51:50 ******/
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
/****** Object:  Table [dbo].[Blog]    Script Date: 18.06.2017 13:51:50 ******/
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
)
)

GO
/****** Object:  Table [dbo].[Comment]    Script Date: 18.06.2017 13:51:50 ******/
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
)
)

GO
/****** Object:  Table [dbo].[Post]    Script Date: 18.06.2017 13:51:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
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
)
)

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[Tag]    Script Date: 18.06.2017 13:51:50 ******/
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
)
) 

GO
SET IDENTITY_INSERT [dbo].[Blog] ON 

INSERT [dbo].[Blog] ([id], [name], [title], [description], [owner], [createdAt], [modifiedAt], [modifiedBy]) VALUES (1, N'functional-data-access-with-sqlfun', N'Functional data access with SqlFun', N'Designing functional-relational mapper with F#', N'jacentino', CAST(0x0000A7810163AEB0 AS DateTime), NULL, NULL)
SET IDENTITY_INSERT [dbo].[Blog] OFF
SET IDENTITY_INSERT [dbo].[Comment] ON 

INSERT [dbo].[Comment] ([id], [postId], [parentId], [content], [author], [createdAt]) VALUES (1, 1, NULL, N'Great, informative article!', N'joeblack', CAST(0x0000A783011494B0 AS DateTime))
INSERT [dbo].[Comment] ([id], [postId], [parentId], [content], [author], [createdAt]) VALUES (2, 1, 1, N'Thank you!', N'jacenty', CAST(0x0000A78500CF5DF0 AS DateTime))
INSERT [dbo].[Comment] ([id], [postId], [parentId], [content], [author], [createdAt]) VALUES (3, 1, 2, N'You''re welcome!', N'joeblack', CAST(0x0000A78601243C80 AS DateTime))
SET IDENTITY_INSERT [dbo].[Comment] OFF
SET IDENTITY_INSERT [dbo].[Post] ON 

INSERT [dbo].[Post] ([id], [blogId], [name], [title], [content], [author], [createdAt], [modifiedAt], [modifiedBy], [status]) VALUES (1, 1, N'another-sql-framework', N'Yet another sql framework', N'There are so many solutions for this problem. What is the case for another one?', N'jacenty', CAST(0x0000A782016EAB30 AS DateTime), NULL, NULL, N'P')
INSERT [dbo].[Post] ([id], [blogId], [name], [title], [content], [author], [createdAt], [modifiedAt], [modifiedBy], [status]) VALUES (2, 1, N'whats-wrong-with-existing-f', N'What''s wrong with existing frameworks', N'Shortly - they not align with functional paradigm.', N'jacenty', CAST(0x0000A78A01391C40 AS DateTime), NULL, NULL, N'P')
SET IDENTITY_INSERT [dbo].[Post] OFF
INSERT [dbo].[Tag] ([postId], [name]) VALUES (1, N'existing')
INSERT [dbo].[Tag] ([postId], [name]) VALUES (1, N'framework')
INSERT [dbo].[Tag] ([postId], [name]) VALUES (1, N'options')
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_Blog_1]    Script Date: 18.06.2017 13:51:50 ******/
ALTER TABLE [dbo].[Blog] ADD  CONSTRAINT [IX_Blog_1] UNIQUE NONCLUSTERED 
(
	[name] ASC
)
GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_Post]    Script Date: 18.06.2017 13:51:50 ******/
ALTER TABLE [dbo].[Post] ADD  CONSTRAINT [IX_Post] UNIQUE NONCLUSTERED 
(
	[blogId] ASC,
	[name] ASC
)
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
USE [master]
GO
ALTER DATABASE [SqlFunTests] SET  READ_WRITE 
GO
