import { useState, type FormEvent } from 'react';

interface SearchBoxProps {
  placeholder: string;
  /** Called with the trimmed text when the user presses Enter (empty clears the search). */
  onSearch: (text: string) => void;
}

/** A search field above a server-filtered list. Searches on Enter, not on every key. */
export function SearchBox({ placeholder, onSearch }: SearchBoxProps) {
  const [text, setText] = useState('');

  const submit = (event: FormEvent) => {
    event.preventDefault();
    onSearch(text.trim());
  };

  return (
    <form className="row" role="search" onSubmit={submit}>
      <input
        className="input input-search"
        type="search"
        placeholder={placeholder}
        aria-label={placeholder}
        value={text}
        onChange={(event) => setText(event.target.value)}
      />
    </form>
  );
}
