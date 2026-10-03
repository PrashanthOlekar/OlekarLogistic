import { useState, type FormEvent } from 'react';
import { Alert, Button, EmptyCard, Loading, PageHead, Pill } from '../../components';
import { useAction } from '../../hooks/useAction';
import { useLoad } from '../../hooks/useLoad';
import { api } from '../../lib/api';
import { date, dateTime } from '../../lib/format';
import { useToast } from '../../state/ToastContext';
import type { AdminUser } from './types';

const ROLES = ['Customer', 'Owner', 'Driver', 'Admin'];

export function UsersPage() {
  const toast = useToast();
  const [role, setRole] = useState('');
  const [searchText, setSearchText] = useState('');
  const [query, setQuery] = useState('');
  const block = useAction();

  const users = useLoad(() => {
    const params = new URLSearchParams();
    if (role) params.set('role', role);
    if (query) params.set('q', query);
    return api<AdminUser[]>(`/admin/users?${params}`);
  }, [role, query]);

  const search = (event: FormEvent) => {
    event.preventDefault();
    setQuery(searchText);
  };

  const toggleBlocked = (user: AdminUser) =>
    block.run(async () => {
      const isBlocked = user.status === 'Blocked';
      await api(`/admin/users/${user.id}/block`, { body: { block: !isBlocked } });
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
            onChange={(event) => setRole(event.target.value)}
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

      {users.data?.length === 0 && <EmptyCard title="No users match" />}

      {!!users.data?.length && (
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
              {users.data.map((user) => (
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
        </div>
      )}
    </>
  );
}
