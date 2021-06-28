composer archive create -t dir -n .

  cd ~/fabric-tools
    export FABRIC_VERSION=hlfv11
    ./startFabric.sh
    ./createPeerAdminCard.sh


composer runtime install --card PeerAdmin@hlfv1 --businessNetworkName docchain1


composer network start --card PeerAdmin@hlfv1 --networkAdmin admin --networkAdminEnrollSecret adminpw --archiveFile docchain1@0.0.1.bna --file networkadmin.card

composer card import --file networkadmin.card

composer-rest-server

yo hyperledger-composer


cd *angular*

npm start

set up port forwarding per
https://stackoverflow.com/questions/9537751/virtualbox-port-forward-from-guest-to-host?utm_medium=organic&utm_source=google_rich_qa&utm_campaign=google_rich_qa