﻿namespace FaithlifeReader.Functions.Dtos;

public class NewsFeedDto
{
	[AllowNull]
	public List<ItemDto> Items { get; set; }
}
