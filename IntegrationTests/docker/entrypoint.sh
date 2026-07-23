#!/bin/bash
set -e

# Install any additional YANG modules from runtime volume (optional)
if [ -d /yang-modules ] && [ "$(ls -A /yang-modules/*.yang 2>/dev/null)" ]; then
    /opt/install-yang-modules.sh /yang-modules
fi

# Start sysrepo daemon in background
sysrepod -d -v 2 2>&1 &
sleep 2

# Grant full NACM access to the netconf user for integration testing
sysrepocfg --import --datastore running --format xml --module ietf-netconf-acm <<'EOF'
<nacm xmlns="urn:ietf:params:xml:ns:yang:ietf-netconf-acm">
  <enable-nacm>true</enable-nacm>
  <read-default>permit</read-default>
  <write-default>permit</write-default>
  <exec-default>permit</exec-default>
</nacm>
EOF

# Start netopeer2 server in foreground (listens on port 830)
exec netopeer2-server -d -v 2
