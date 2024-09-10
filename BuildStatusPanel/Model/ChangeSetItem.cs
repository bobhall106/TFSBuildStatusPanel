using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace BuildStatusPanel.Model
{
    public class ChangeSetItem
    {
        private Changeset _tfsChangeset = null;

        public string Comment
        {
            get
            {
                if (_tfsChangeset == null)
                    return string.Empty;
                return _tfsChangeset.Comment;
            }
        }

        public string ChangesetId
        {
            get
            {
                if (_tfsChangeset == null)
                    return string.Empty;
                return _tfsChangeset.ChangesetId.ToString();
            }
        }

        public string Owner
        {
            get
            {
                if ((_tfsChangeset == null) || (string.IsNullOrEmpty(_tfsChangeset.Owner)))
                    return string.Empty;
                return _tfsChangeset.Owner.Replace($"{GlobalDefaults.CompanyName}\\", "");
            }
        }

        public string CreationDate
        {
            get
            {
                if (_tfsChangeset == null)
                    return string.Empty;
                return _tfsChangeset.CreationDate.ToString();
            }
        }

        public DateTime CreationDateTime
        {
            get
            {
                if (_tfsChangeset == null)
                    return DateTime.MinValue;
                return _tfsChangeset.CreationDate;
            }
        }

        public string Information
        {
            get
            {
                if (_tfsChangeset == null)
                    return string.Empty;
                return _tfsChangeset.Comment;//for now
            }
        }

        public string PolicyOverrideComment
        {
            get
            {
                if ((_tfsChangeset == null) || (_tfsChangeset.PolicyOverride == null) || (string.IsNullOrEmpty(_tfsChangeset.PolicyOverride.Comment)))
                    return string.Empty;
                return _tfsChangeset.PolicyOverride.Comment;
            }
        }

        public int ChangesCount
        {
            get
            {
                if ((_tfsChangeset == null) || (_tfsChangeset.Changes == null))
                    return 0;
                return _tfsChangeset.Changes.Count();
            }
        }

        public ChangeSetItem(Changeset cs)
        {
            _tfsChangeset = cs;
        }
    }
}