using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class MessageRepository : IMessageRepository
    {
        private readonly DataContext _dataContext;
        private readonly IMapper _mapper;

        public MessageRepository(DataContext dataContext, IMapper mapper)
        {
            _mapper = mapper;
            _dataContext = dataContext;
        }

        public void AddGroup(Group group)
        {
            _dataContext.Groups.Add(group);
        }

        public void AddMessage(Message message)
        {
            _dataContext.Messages.Add(message);
        }

        public void DeleteMessage(Message message)
        {
            _dataContext.Messages.Remove(message);
        }

        public async Task<Connection> GetConnection(string connectionId)
        {
            return await _dataContext.Connections.FindAsync(connectionId);
        }

        public async Task<Group> GetGroupForConnection(string connectionId)
        {
            return await _dataContext.Groups
                .Include(c => c.Connections)
                .Where(c => c.Connections.Any(x => x.ConnectionId == connectionId))
                .FirstOrDefaultAsync();
        }

        public async Task<Message> GetMessage(int id)
        {
            return await _dataContext.Messages
                .Include(u => u.Sender)
                .Include(u => u.Recipient)
                .SingleOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Group> GetMessageGroup(string groupName)
        {
            return await _dataContext.Groups
                .Include(x => x.Connections)
                .FirstOrDefaultAsync(x => x.Name == groupName);
        }

        public async Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
        {
            var query = _dataContext.Messages
                .OrderByDescending(m => m.MessageSent)
                .ProjectTo<MessageDto>(_mapper.ConfigurationProvider)
                .AsQueryable();

            query = messageParams.Container switch
            {
                "Inbox" => query.Where(u => u.RecipientUsername == messageParams.Username
                    && !u.RecipientDeleted),
                "Outbox" => query.Where(u => u.SenderUsername == messageParams.Username
                    && !u.SenderDeleted),
                _ => query.Where(u => u.RecipientUsername == messageParams.Username
                    && !u.RecipientDeleted
                    && u.DateRead == null)
            };

            return await PagedList<MessageDto>.CreateAsync(query, messageParams.PageNumber,
                messageParams.PageSize);
        }

        public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUsername, 
            string recipientUsername)
        {

            var messages = await _dataContext.Messages
                .Where(m => m.Recipient.UserName == currentUsername
                        && m.Sender.UserName == recipientUsername
                        && !m.RecipientDeleted
                        || m.Recipient.UserName == recipientUsername
                        && m.Sender.UserName == currentUsername
                        && !m.SenderDeleted)
                .OrderBy(m => m.MessageSent)
                .ProjectTo<MessageDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var unreadMessages = messages.Where(m => m.DateRead == null
                && m.RecipientUsername == currentUsername).ToList();

            if (unreadMessages.Any())
            {
                foreach (var message in unreadMessages)
                {
                    message.DateRead = DateTime.UtcNow;
                }
            }

            return messages;
            
        }

        public void RemoveConnection(Connection connection)
        {
            _dataContext.Connections.Remove(connection);
        }
    }
}