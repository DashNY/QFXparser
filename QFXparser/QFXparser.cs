﻿using System.IO;
using System.Linq;
using System.Reflection;

namespace QFXparser
{
    public class FileParser
    {        
        private string _fileText;

        public FileParser(string fileNamePath)
        {
            using (StreamReader sr = new StreamReader(fileNamePath,true))
            {
                _fileText = sr.ReadToEnd();
            }

        }

        public FileParser(Stream fileStream)
        {
            using (StreamReader sr = new StreamReader(fileStream,true))
            {
                _fileText = sr.ReadToEnd();
            }

        }

        public Statement BuildStatement()
        {
            RawStatement rawStatement = BuildRaw();

            Statement statement = new Statement()
            {
                AccountNum = rawStatement.AccountNum
            };

            foreach (var rawTrans in rawStatement.Transactions)
            {
                Transaction trans = new Transaction()
                {
                    Amount = rawTrans.Amount,
                    Memo = rawTrans.Memo,
                    Name = rawTrans.Name,
                    PostedOn = rawTrans.DatePosted,
                    RefNumber = rawTrans.RefNumber,
                    TransactionId = rawTrans.TransactionId,
                    Type = rawTrans.Type
                };
                statement.Transactions.Add(trans);
            }

            return statement;
        }

        private RawStatement BuildRaw()
        {
            RawStatement _statement = null;
            MemberInfo currentMember = null;
            RawTransaction _currentTransaction = null;

            foreach (var token in Parser.Parse(_fileText))
            {
                if (token.IsElement)
                {
                    var result = GetPropertyInfo(token.Content);
                    if (result != null)
                    {
                        switch (result.Type)
                        {
                            case NodeType.StatementOpen:
                                _statement = new RawStatement();
                                break;
                            case NodeType.StatementClose:
                                return _statement;
                                break;
                            case NodeType.TransactionOpen:
                                _currentTransaction = new RawTransaction();
                                break;
                            case NodeType.TransactionClose:
                                _statement.Transactions.Add(_currentTransaction);
                                _currentTransaction = null;
                                break;
                            case NodeType.StatementProp:
                                if (_statement == null)
                                {
                                    _statement = new RawStatement();
                                }
                                currentMember = result.Member;
                                break;
                            case NodeType.TransactionProp:
                                currentMember = result.Member;
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        currentMember = null;
                    }
                }
                else
                {
                    if (currentMember != null && currentMember is PropertyInfo)
                    {
                        var property = (PropertyInfo)currentMember;
                        switch (property.DeclaringType.Name)
                        {
                            case "RawStatement":
                                property.SetValue(_statement, token.Content);
                                break;
                            case "RawTransaction":
                                property.SetValue(_currentTransaction, token.Content);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            return _statement;
        }

        private PropertyResult GetPropertyInfo(string token)
        {
            var propertyResult = new PropertyResult();

            if (typeof(RawStatement).GetCustomAttribute<NodeNameAttribute>().CloseTag == token)
            {
                propertyResult.Member = typeof(RawStatement);
                propertyResult.Type = NodeType.StatementClose;
                return propertyResult;
            }

            if (typeof(RawTransaction).GetCustomAttribute<NodeNameAttribute>().CloseTag == token)
            {
                propertyResult.Member = typeof(RawTransaction);
                propertyResult.Type = NodeType.TransactionClose;
                return propertyResult;
            }

            if (typeof(RawStatement).GetCustomAttribute<NodeNameAttribute>().OpenTag == token)
            {
                propertyResult.Member = typeof(RawStatement);
                propertyResult.Type = NodeType.StatementOpen;
                return propertyResult;
            }

            if (typeof(RawTransaction).GetCustomAttribute<NodeNameAttribute>().OpenTag == token)
            {
                propertyResult.Member = typeof(RawTransaction);
                propertyResult.Type = NodeType.TransactionOpen;
                return propertyResult;
            }


            var statementMember = typeof(RawStatement).GetProperties().FirstOrDefault(m => m.GetCustomAttribute<NodeNameAttribute>().OpenTag == token);

            if (statementMember != null)
            {
                propertyResult.Member = statementMember;
                propertyResult.Type = NodeType.StatementProp;
                return propertyResult;
            }

            var transactionMember = typeof(RawTransaction).GetProperties().Where(m => m.GetCustomAttribute<NodeNameAttribute>() != null)
                .FirstOrDefault(m => m.GetCustomAttribute<NodeNameAttribute>().OpenTag == token);

            if (transactionMember != null)
            {
                propertyResult.Member = transactionMember;
                propertyResult.Type = NodeType.TransactionProp;
                return propertyResult;
            }

            return null;

        }

    }    
}
