import { useState, type FormEvent } from 'react';
import { usersApi } from '../../api/adminApi';
import { Alert, Button, EmptyCard, Loading, PageHead, Pagination, Pill, useToast } from '../../components';
import { useAction } from '../../hooks/useAction';
import { usePagedLoad } from '../../hooks/usePagedLoad';
import { ROLES, type Role, type UserListItem } from '../../types';
import { date, dateTime } from '../../utils/format';

export function UsersPage() {
  const toast = useToast();
  const [role, setRole] = useState<Role | ''>('');
  const [searchText, setSearchText] = useState('');
  const [query, setQuery] = useState('');
  const block = useAction();

  const users = usePagedLoad((page) => usersApi.list({ ...page, role, search: query }), [role, query]);

  const search = (event: FormEvent) => {
    event.preventDefault();
    setQuery(searchText.trim());
  };

  const toggleBlocked = (user: UserListItem) =>
    block.run(async () => {
      const isBlocked = user.status === 'Blocked';
      await usersApi.setStatus(user.id, isBlocked ? 'Active' : 'Blocked');
      toast(isBlocked ? `${user.fullName} unblocked.` : `${user.fullName} blocked. They are signed out now.`);
      users.reload();
    });

  return (
    <>
      <PageHead
        title="Users"
        sub="Block accounts that show fraud or abuse. Blocking takes effect immediately."
      >
        <form className="row" onSubmit={search}>
          <input
            className="input input-search"
            placeholder="Name or mobile"
            aria-label="Search users"
            value={searchText}
            onChange={(event) => setSearchText(event.target.value)}
          />
          <select
            className="input input-narrow"
            aria-label="Role"
            value={role}
            onChange={(event) => setRole(event.target.value as Role | '')}
          >
            <option value="">All roles</option>
            {ROLES.map((option) => (
              <option key={option}>{option}</option>
            ))}
          </select>
        </form>
      </PageHead>

      {block.error && (
        <Alert kind="error" className="mb-sm">
          {block.error}
        </Alert>
      )}

      <Loading state={users} />

      {users.items?.length === 0 && <EmptyCard title="No users match" />}

      {!!users.items?.length && (
        <div className="card table-wrap">
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Role</th>
                <th>Mobile</th>
                <th>Joined</th>
                <th>Last sign-in</th>
                <th>Status</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {users.items.map((user) => (
                <tr key={user.id}>
                  <td>
                    <b>{user.fullName}</b>
                    {user.email && <div className="small muted">{user.email}</div>}
                  </td>
                  <td>{user.role}</td>
                  <td className="mono">{user.mobile}</td>
                  <td>{date(user.createdAt)}</td>
                  <td>{dateTime(user.lastLoginAt)}</td>
                  <td>
                    <Pill status={user.status} />
                  </td>
                  <td>
                    {user.role !== 'Admin' && (
                      <Button
                        size="sm"
                        variant={user.status === 'Blocked' ? 'secondary' : 'danger'}
                        busy={block.busy}
                        onClick={() => toggleBlocked(user)}
                      >
                        {user.status === 'Blocked' ? 'Unblock' : 'Block'}
                      </Button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <Pagination page={users.data} onPageChange={users.setPageNumber} />
        </div>
      )}
    </>
  );
}
