import { NavLink, useParams } from 'react-router-dom'
import { Empty } from '../../ui/feedback'
import { PageTitle } from '../../ui/primitives'
import { Account } from './Account'
import { Assistant, Pomodoro } from './Preferences'
import { Privacy } from './Privacy'
import { WorkProfiles } from './WorkProfiles'

const sections = [
  { path: '', title: 'Account', Component: Account },
  { path: 'work-profiles', title: 'Work profiles', Component: WorkProfiles },
  { path: 'pomodoro', title: 'Pomodoro', Component: Pomodoro },
  { path: 'privacy', title: 'Privacy', Component: Privacy },
  { path: 'assistant', title: 'Assistant', Component: Assistant },
]

export default function Settings() {
  const { section = '' } = useParams()
  const selected = sections.find((item) => item.path === section)
  const Panel = selected?.Component
  return (
    <>
      <PageTitle title="Settings">Manage your workspace and privacy preferences.</PageTitle>
      <nav aria-label="Settings sections" className="settings-tabs">
        {sections.map(({ path, title }) => (
          <NavLink
            end
            className="button secondary"
            key={path}
            to={'/settings' + (path ? '/' + path : '')}
          >
            {title}
          </NavLink>
        ))}
      </nav>
      <div className="settings-main">
        {Panel ? <Panel /> : <Empty title="Settings section not found" />}
      </div>
    </>
  )
}
