-- =====================================================================================
-- Eliminazione account dall'app (OBBLIGATORIA per App Store e Google Play).
-- Eseguire UNA VOLTA nel SQL Editor di Supabase (Dashboard -> SQL Editor -> New query).
--
-- La funzione gira con i privilegi del proprietario (security definer) ma cancella
-- SOLO l'utente che la chiama (auth.uid()), quindi un utente non può eliminare altri.
-- Le righe collegate (profiles, campaign_members, personal_notes...) vengono rimosse
-- se le foreign key verso auth.users / profiles sono ON DELETE CASCADE; in caso contrario
-- aggiungi qui sotto le DELETE esplicite prima della riga su auth.users.
-- =====================================================================================

create or replace function public.delete_my_account()
returns void
language plpgsql
security definer
set search_path = public, auth
as $$
declare
  uid uuid := auth.uid();
begin
  if uid is null then
    raise exception 'Non autenticato';
  end if;

  -- dati personali dell'utente
  delete from public.personal_notes   where user_id  = uid;
  delete from public.shared_files     where owner_id = uid;
  delete from public.campaign_members where user_id  = uid;
  update public.characters set owner_id = null where owner_id = uid;
  delete from public.profiles         where id       = uid;

  -- account di autenticazione
  delete from auth.users where id = uid;
end;
$$;

revoke all on function public.delete_my_account() from public, anon;
grant execute on function public.delete_my_account() to authenticated;
